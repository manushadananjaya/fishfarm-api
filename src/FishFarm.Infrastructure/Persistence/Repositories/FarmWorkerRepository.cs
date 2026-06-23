using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class FarmWorkerRepository : BaseRepository<FarmWorker>, IFarmWorkerRepository
{
    public FarmWorkerRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<FarmWorker> Items, int TotalCount)> GetPagedByFarmAsync(
        Guid            fishFarmId,
        int             pageNumber,
        int             pageSize,
        string?         search      = null,
        WorkerPosition? position    = null,
        bool?           certExpired = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(fw => fw.Person)
            .Where(fw => fw.FishFarmId == fishFarmId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(fw =>
                fw.Person.Name.Contains(search) ||
                fw.Person.Email.Contains(search));

        if (position.HasValue)
            query = query.Where(fw => fw.Position == position.Value);

        if (certExpired.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = certExpired.Value
                ? query.Where(fw => fw.Person.CertifiedUntil < today)
                : query.Where(fw => fw.Person.CertifiedUntil >= today);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(fw => fw.Person.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<FarmWorker?> GetByIdAndFarmAsync(
        Guid farmWorkerId,
        Guid fishFarmId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(fw => fw.Person)
            .FirstOrDefaultAsync(
                fw => fw.Id == farmWorkerId && fw.FishFarmId == fishFarmId,
                cancellationToken);

    public async Task<bool> HasCeoAsync(
        Guid  fishFarmId,
        Guid? excludeFarmWorkerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(fw =>
            fw.FishFarmId == fishFarmId &&
            fw.Position   == WorkerPosition.CEO);

        if (excludeFarmWorkerId.HasValue)
            query = query.Where(fw => fw.Id != excludeFarmWorkerId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsPersonAssignedAsync(
        Guid fishFarmId,
        Guid personId,
        CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(
            fw => fw.FishFarmId == fishFarmId && fw.PersonId == personId,
            cancellationToken);
}
