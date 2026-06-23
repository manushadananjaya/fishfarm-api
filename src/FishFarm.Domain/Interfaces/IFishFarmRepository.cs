using FishFarmEntity = FishFarm.Domain.Entities.FishFarm;

namespace FishFarm.Domain.Interfaces;

public interface IFishFarmRepository : IRepository<FishFarmEntity>
{
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
