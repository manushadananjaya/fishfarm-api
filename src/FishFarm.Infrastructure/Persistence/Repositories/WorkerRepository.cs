using FishFarm.Domain.Entities;
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
}
