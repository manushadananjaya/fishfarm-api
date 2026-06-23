using FishFarm.Domain.Common;
using FishFarmEntity = FishFarm.Domain.Entities.FishFarm;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Fish farm-specific repository with optimised query methods.
/// </summary>
public interface IFishFarmRepository : IRepository<FishFarmEntity>
{
    /// <summary>
    /// Returns full farm entities (with FarmWorkers + Person eagerly loaded) for map display.
    /// The Application layer projects these into <c>FishFarmMapDto</c>.
    /// Optionally filtered to a geographic bounding box.
    /// </summary>
    Task<IReadOnlyList<FishFarmEntity>> GetMapAsync(
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
