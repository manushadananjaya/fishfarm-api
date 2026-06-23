using FishFarm.Domain.Common;
using FishFarmEntity = FishFarm.Domain.Entities.FishFarm;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Fish farm-specific repository with optimized query methods.
/// </summary>
public interface IFishFarmRepository : IRepository<FishFarmEntity>
{
    /// <summary>
    /// Returns lightweight GPS projections for map display.
    /// Applies an optional bounding-box filter entirely in SQL — no full entity load.
    /// The global soft-delete query filter is active; deleted farms are excluded.
    /// </summary>
    Task<IReadOnlyList<FishFarmMapPoint>> GetMapAsync(
        decimal? north = null,
        decimal? south = null,
        decimal? east  = null,
        decimal? west  = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)> Items, int TotalCount)> GetPagedAsync(
        int     pageNumber,
        int     pageSize,
        string? search    = null,
        bool?   hasBarge  = null,
        int?    minCages  = null,
        int?    maxCages  = null,
        string  sortBy    = "name",
        string  sortDir   = "asc",
        CancellationToken cancellationToken = default);

    Task<FishFarmEntity?> GetWithWorkersAsync(Guid id, CancellationToken cancellationToken = default);
}
