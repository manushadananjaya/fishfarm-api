using FishFarmEntity = FishFarm.Domain.Entities.FishFarm;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Fish farm-specific repository with optimized query methods.
/// </summary>
public interface IFishFarmRepository : IRepository<FishFarmEntity>
{
    /// <summary>
    /// Paginated list of farms with a lightweight workers summary.
    /// Does NOT include full worker details for performance.
    /// </summary>
    Task<(IReadOnlyList<FishFarmEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full farm detail including all active workers.
    /// </summary>
    Task<FishFarmEntity?> GetWithWorkersAsync(Guid id, CancellationToken cancellationToken = default);
}
