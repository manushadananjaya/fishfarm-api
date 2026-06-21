using FishFarmEntity = FishFarm.Domain.Entities.FishFarm;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Fish farm-specific repository with optimized query methods.
/// </summary>
public interface IFishFarmRepository : IRepository<FishFarmEntity>
{
    Task<(IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<FishFarmEntity?> GetWithWorkersAsync(Guid id, CancellationToken cancellationToken = default);
}
