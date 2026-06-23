using FishFarm.Domain.Interfaces;

namespace FishFarm.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IFishFarmRepository    FishFarms    { get; }
    public IPersonRepository      People       { get; }
    public IFarmWorkerRepository  FarmWorkers  { get; }

    public UnitOfWork(
        AppDbContext           context,
        IFishFarmRepository    fishFarms,
        IPersonRepository      people,
        IFarmWorkerRepository  farmWorkers)
    {
        _context    = context;
        FishFarms   = fishFarms;
        People      = people;
        FarmWorkers = farmWorkers;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync()
        => await _context.DisposeAsync();
}
