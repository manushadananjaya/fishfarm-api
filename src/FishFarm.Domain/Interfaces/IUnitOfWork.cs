namespace FishFarm.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IFishFarmRepository  FishFarms   { get; }
    IPersonRepository    People      { get; }
    IFarmWorkerRepository FarmWorkers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
