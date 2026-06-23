namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Coordinates multiple repositories inside a single database transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IFishFarmRepository  FishFarms   { get; }
    IPersonRepository    People      { get; }
    IFarmWorkerRepository FarmWorkers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
