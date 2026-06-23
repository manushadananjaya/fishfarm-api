using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class FishFarmRepository
    : BaseRepository<Domain.Entities.FishFarm>, IFishFarmRepository
{
    public FishFarmRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Domain.Entities.FishFarm>> GetMapAsync(
        decimal? north = null,
        decimal? south = null,
        decimal? east  = null,
        decimal? west  = null,
        CancellationToken cancellationToken = default)
    {
        // Explicit IQueryable<> type lets us reassign after Where() without a cast conflict.
        // Include FarmWorkers only — Person detail is not needed for a worker count.
        IQueryable<Domain.Entities.FishFarm> query = DbSet
            .AsNoTracking()
            .Include(f => f.FarmWorkers);

        // Apply bounding-box filters in SQL — the full table is never loaded into memory.
        if (north.HasValue) query = query.Where(f => f.GpsLatitude  <= north.Value);
        if (south.HasValue) query = query.Where(f => f.GpsLatitude  >= south.Value);
        if (east.HasValue)  query = query.Where(f => f.GpsLongitude <= east.Value);
        if (west.HasValue)  query = query.Where(f => f.GpsLongitude >= west.Value);

        return await query
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

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

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        if (hasBarge.HasValue)
            query = query.Where(f => f.HasBarge == hasBarge.Value);

        if (minCages.HasValue)
            query = query.Where(f => f.NumberOfCages >= minCages.Value);

        if (maxCages.HasValue)
            query = query.Where(f => f.NumberOfCages <= maxCages.Value);

        // Worker count from FarmWorkers (global filter already excludes soft-deleted assignments)
        var projected = query.Select(f => new
        {
            Farm        = f,
            WorkerCount = f.FarmWorkers.Count()
        });

        bool asc = sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase);
        projected = sortBy.ToLowerInvariant() switch
        {
            "createdat"     => asc ? projected.OrderBy(x => x.Farm.CreatedAt)     : projected.OrderByDescending(x => x.Farm.CreatedAt),
            "updatedat"     => asc ? projected.OrderBy(x => x.Farm.UpdatedAt)     : projected.OrderByDescending(x => x.Farm.UpdatedAt),
            "numberofcages" => asc ? projected.OrderBy(x => x.Farm.NumberOfCages) : projected.OrderByDescending(x => x.Farm.NumberOfCages),
            "workercount"   => asc ? projected.OrderBy(x => x.WorkerCount)        : projected.OrderByDescending(x => x.WorkerCount),
            _               => asc ? projected.OrderBy(x => x.Farm.Name)          : projected.OrderByDescending(x => x.Farm.Name),
        };

        var total = await projected.CountAsync(cancellationToken);
        var raw   = await projected
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = raw.Select(x => (x.Farm, x.WorkerCount)).ToList();
        return (items, total);
    }

    public async Task<Domain.Entities.FishFarm?> GetWithFarmWorkersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(f => f.FarmWorkers)
                .ThenInclude(fw => fw.Person)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
}
