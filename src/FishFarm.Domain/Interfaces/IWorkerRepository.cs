using FishFarm.Domain.Entities;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Worker-specific repository.
/// </summary>
public interface IWorkerRepository : IRepository<Worker>
{
    Task<(IReadOnlyList<Worker> Items, int TotalCount)> GetPagedByFishFarmAsync(
        Guid fishFarmId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Worker?> GetByIdAndFarmAsync(
        Guid workerId,
        Guid fishFarmId,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeWorkerId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasCeoAsync(
        Guid fishFarmId,
        Guid? excludeWorkerId = null,
        CancellationToken cancellationToken = default);
}
