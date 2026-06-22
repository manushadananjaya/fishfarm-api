using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class WorkerRepository : BaseRepository<Worker>, IWorkerRepository
{
    public WorkerRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Worker> Items, int TotalCount)> GetPagedByFishFarmAsync(
        Guid fishFarmId,
        int pageNumber,
        int pageSize,
        string?         search      = null,
        WorkerPosition? position    = null,
        bool?           certExpired = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(w => w.FishFarmId == fishFarmId);

        // All filters applied before CountAsync so TotalCount reflects the filtered set.
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Name.Contains(search) || w.Email.Contains(search));

        if (position.HasValue)
            query = query.Where(w => w.Position == position.Value);

        if (certExpired.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = certExpired.Value
                ? query.Where(w => w.CertifiedUntil < today)
                : query.Where(w => w.CertifiedUntil >= today);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(w => w.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<Worker?> GetByIdAndFarmAsync(
        Guid workerId,
        Guid fishFarmId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(
                w => w.Id == workerId && w.FishFarmId == fishFarmId,
                cancellationToken);

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeWorkerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(w => w.Email == email);

        if (excludeWorkerId.HasValue)
            query = query.Where(w => w.Id != excludeWorkerId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasCeoAsync(
        Guid fishFarmId,
        Guid? excludeWorkerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(w =>
            w.FishFarmId == fishFarmId &&
            w.Position   == WorkerPosition.CEO);

        if (excludeWorkerId.HasValue)
            query = query.Where(w => w.Id != excludeWorkerId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
