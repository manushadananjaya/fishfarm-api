using FishFarmEntity = FishFarm.Domain.Entities.FishFarm;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Fish farm-specific repository with optimised query methods.
/// </summary>
public interface IFishFarmRepository : IRepository<FishFarmEntity>
{
    /// <summary>
    /// Returns farm entities with a pre-computed active <c>WorkerCount</c> for map display.
    /// WorkerCount is projected as a SQL COUNT subquery — FarmWorkers are never loaded into memory.
    /// Optionally filtered to a geographic bounding box.
    /// </summary>
    Task<IReadOnlyList<(FishFarmEntity Farm, int WorkerCount)>> GetMapAsync(
        decimal? north = null,
        decimal? south = null,
        decimal? east  = null,
        decimal? west  = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<(FishFarmEntity Farm, int WorkerCount)> Items, int TotalCount)> GetPagedAsync(
        int     pageNumber,
        int     pageSize,
        string? search    = null,
        bool?   hasBarge  = null,
        int?    minCages  = null,
        int?    maxCages  = null,
        string  sortBy    = "name",
        string  sortDir   = "asc",
        CancellationToken cancellationToken = default);

    /// <summary>Farm with all active FarmWorker assignments and each worker's Person loaded.</summary>
    Task<FishFarmEntity?> GetWithFarmWorkersAsync(Guid id, CancellationToken cancellationToken = default);
}
