using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class FishFarmRepository
    : BaseRepository<Domain.Entities.FishFarm>, IFishFarmRepository
{
    public FishFarmRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)> Items, int TotalCount)>
        GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search    = null,
            bool?   hasBarge  = null,
            int?    minCages  = null,
            int?    maxCages  = null,
            CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        // All filters are applied before CountAsync so TotalCount always reflects
        // the filtered result set, not the full table size.
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        if (hasBarge.HasValue)
            query = query.Where(f => f.HasBarge == hasBarge.Value);

        if (minCages.HasValue)
            query = query.Where(f => f.NumberOfCages >= minCages.Value);

        if (maxCages.HasValue)
            query = query.Where(f => f.NumberOfCages <= maxCages.Value);

        var total = await query.CountAsync(cancellationToken);

        // Project worker count in-database via a correlated COUNT subquery.
        // This avoids loading all worker rows just to count them.
        var projected = await query
            .OrderBy(f => f.Name)
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
