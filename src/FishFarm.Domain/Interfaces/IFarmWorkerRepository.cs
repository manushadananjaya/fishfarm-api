using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;

namespace FishFarm.Domain.Interfaces;

public interface IFarmWorkerRepository : IRepository<FarmWorker>
{
    /// <summary>Paginated list of assignments for a specific farm, with Person details loaded.</summary>
    Task<(IReadOnlyList<FarmWorker> Items, int TotalCount)> GetPagedByFarmAsync(
        Guid            fishFarmId,
        int             pageNumber,
        int             pageSize,
        string?         search      = null,
        WorkerPosition? position    = null,
        bool?           certExpired = null,
        CancellationToken cancellationToken = default);

    /// <summary>Single assignment scoped to its farm, with Person loaded.</summary>
    Task<FarmWorker?> GetByIdAndFarmAsync(
        Guid farmWorkerId,
        Guid fishFarmId,
        CancellationToken cancellationToken = default);

    /// <summary>True if the farm already has an active CEO (optionally excluding one record).</summary>
    Task<bool> HasCeoAsync(
        Guid  fishFarmId,
        Guid? excludeFarmWorkerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>True if a person is already assigned (active) to a specific farm.</summary>
    Task<bool> IsPersonAssignedAsync(
        Guid fishFarmId,
        Guid personId,
        CancellationToken cancellationToken = default);
}
