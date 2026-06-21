using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class FishFarmRepository
    : BaseRepository<Domain.Entities.FishFarm>, IFishFarmRepository
{
    public FishFarmRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Domain.Entities.FishFarm> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(f => f.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            // Include workers only for count; project or load separately for full detail
            .Include(f => f.Workers)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Domain.Entities.FishFarm?> GetWithWorkersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(f => f.Workers)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
}
