using FishFarm.Domain.Entities;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Worker-specific repository.
/// </summary>
public interface IWorkerRepository : IRepository<Worker>
{
    Task<IReadOnlyList<Worker>> GetByFishFarmAsync(
        Guid fishFarmId,
        CancellationToken cancellationToken = default);

    Task<Worker?> GetByIdAndFarmAsync(
        Guid workerId,
        Guid fishFarmId,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeWorkerId = null,
        CancellationToken cancellationToken = default);
}
