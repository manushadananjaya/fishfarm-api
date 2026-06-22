using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;

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
        string?         search      = null,
        WorkerPosition? position    = null,
        bool?           certExpired = null,
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
