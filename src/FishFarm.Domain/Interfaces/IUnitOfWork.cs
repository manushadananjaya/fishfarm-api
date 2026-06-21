namespace FishFarm.Domain.Interfaces;

/// <summary>
/// Unit of Work: coordinates multiple repositories in a single transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IFishFarmRepository FishFarms { get; }
    IWorkerRepository Workers { get; }

    /// <summary>Commits all pending changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
