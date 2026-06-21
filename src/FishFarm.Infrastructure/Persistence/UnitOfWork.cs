using FishFarm.Domain.Interfaces;

namespace FishFarm.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IFishFarmRepository FishFarms { get; }
    public IWorkerRepository Workers { get; }

    public UnitOfWork(
        AppDbContext context,
        IFishFarmRepository fishFarms,
        IWorkerRepository workers)
    {
        _context  = context;
        FishFarms = fishFarms;
        Workers   = workers;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync()
        => await _context.DisposeAsync();
}
