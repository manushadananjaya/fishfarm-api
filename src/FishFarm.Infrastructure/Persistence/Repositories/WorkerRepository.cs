using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class WorkerRepository : BaseRepository<Worker>, IWorkerRepository
{
    public WorkerRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Worker>> GetByFishFarmAsync(
        Guid fishFarmId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(w => w.FishFarmId == fishFarmId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Worker> Items, int TotalCount)> GetPagedByFishFarmAsync(
        Guid fishFarmId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(w => w.FishFarmId == fishFarmId);
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
