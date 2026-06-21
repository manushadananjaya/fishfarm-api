using FishFarm.Domain.Common;

namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Generic repository contract.
/// Implementations in Infrastructure use EF Core and respect
/// the ISoftDeletable global query filter by default.
/// </summary>
public interface IRepository<T> where T : BaseAuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    /// <summary>
    /// Soft-deletes the entity (sets IsDeleted = true).
    /// The SaveChanges interceptor enforces this at the DbContext level as well.
    /// </summary>
    void Delete(T entity);
}
