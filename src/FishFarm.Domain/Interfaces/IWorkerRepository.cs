using FishFarm.Domain.Entities;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Worker-specific repository.
/// </summary>
public interface IWorkerRepository : IRepository<Worker>
{
    /// <summary>
    /// Returns all active workers for the given farm, ordered by name.
    /// </summary>
    Task<IReadOnlyList<Worker>> GetByFishFarmAsync(
        Guid fishFarmId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single worker scoped to its farm (null if not found or soft-deleted).
    /// </summary>
    Task<Worker?> GetByIdAndFarmAsync(
        Guid workerId,
        Guid fishFarmId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an email is already taken by an active worker on any farm.
    /// Used by FluentValidation to enforce uniqueness.
    /// </summary>
    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeWorkerId = null,
        CancellationToken cancellationToken = default);
}
