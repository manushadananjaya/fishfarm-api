using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class FishFarmRepository
    : BaseRepository<Domain.Entities.FishFarm>, IFishFarmRepository
{
    public FishFarmRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)> Items, int TotalCount)>
        GetPagedAsync(
            int     pageNumber,
            int     pageSize,
            string? search    = null,
            bool?   hasBarge  = null,
            int?    minCages  = null,
            int?    maxCages  = null,
            string  sortBy    = "name",
            string  sortDir   = "asc",
            CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        // All filters applied before CountAsync — TotalCount reflects the filtered set.
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        if (hasBarge.HasValue)
            query = query.Where(f => f.HasBarge == hasBarge.Value);

        if (minCages.HasValue)
            query = query.Where(f => f.NumberOfCages >= minCages.Value);

        if (maxCages.HasValue)
            query = query.Where(f => f.NumberOfCages <= maxCages.Value);

        var total = await query.CountAsync(cancellationToken);

        // Sorting is applied at the SQL level so Skip/Take pages correctly over
        // the full sorted dataset, not just the current page.
        // workerCount uses f.Workers.Count() — EF Core translates to a correlated
        // COUNT subquery which SQL Server can use in ORDER BY.
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Domain.Entities.FishFarm> ordered = sortBy.ToLowerInvariant() switch
        {
            "createdat"     => desc ? query.OrderByDescending(f => f.CreatedAt)      : query.OrderBy(f => f.CreatedAt),
            "updatedat"     => desc ? query.OrderByDescending(f => f.UpdatedAt)      : query.OrderBy(f => f.UpdatedAt),
            "numberofcages" => desc ? query.OrderByDescending(f => f.NumberOfCages)  : query.OrderBy(f => f.NumberOfCages),
            "workercount"   => desc ? query.OrderByDescending(f => f.Workers.Count()): query.OrderBy(f => f.Workers.Count()),
            _               => desc ? query.OrderByDescending(f => f.Name)           : query.OrderBy(f => f.Name),
        };

        var projected = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                Farm        = f,
                WorkerCount = f.Workers.Count()   // EF Core translates to SQL COUNT
            })
            .ToListAsync(cancellationToken);

        var items = projected
            .Select(p => (p.Farm, p.WorkerCount))
            .ToList()
            .AsReadOnly();

        return (items, total);
    }

    public async Task<Domain.Entities.FishFarm?> GetWithWorkersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(f => f.Workers)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
}
