using FishFarm.Domain.Entities;

namespace FishFarm.Domain.Interfaces;

public interface IPersonRepository : IRepository<Person>
{
    Task<(IReadOnlyList<(Person Person, int FarmCount)> Items, int TotalCount)> GetPagedAsync(
        int      pageNumber,
        int      pageSize,
        string?  search      = null,
        bool?    certExpired = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the person with their active FarmWorker assignments (and each farm name).</summary>
    Task<Person?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid?  excludePersonId = null,
        CancellationToken cancellationToken = default);
}
