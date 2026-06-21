using FishFarm.Domain.Common;
using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository.
/// Soft-deletes are enforced by the AuditAndSoftDeleteInterceptor at the DbContext level.
/// Global query filters ensure deleted rows are invisible unless .IgnoreQueryFilters() is used.
/// </summary>
public abstract class BaseRepository<T> : IRepository<T>
    where T : BaseAuditableEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected BaseRepository(AppDbContext context)
    {
        Context = context;
        DbSet   = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity)
        => DbSet.Update(entity);

    /// <summary>
    /// Marks entity as deleted. The interceptor intercepts EntityState.Deleted
    /// and converts it to a soft delete. This method triggers that same path.
    /// </summary>
    public virtual void Delete(T entity)
        => DbSet.Remove(entity);
}
