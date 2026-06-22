using FishFarm.Domain.Common;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Aggregate root representing a fish farm.
/// </summary>
public sealed class FishFarm : BaseAuditableEntity
{
    /// <summary>
    /// DB-generated sequential number used as the human-readable display identifier.
    /// Populated by SQL Server IDENTITY after the first SaveChanges.
    /// Do not set this manually — use the formatted <c>FarmCode</c> ("FF-00001") in DTOs.
    /// </summary>
    public int FarmNumber { get; private set; }

    public string Name { get; set; } = default!;

    public decimal GpsLatitude { get; set; }

    public decimal GpsLongitude { get; set; }

    public int NumberOfCages { get; set; }

    public bool HasBarge { get; set; }

    public string? PictureUrl { get; set; }

    public string? PicturePublicId { get; set; }

    private readonly List<Worker> _workers = [];

    public IReadOnlyCollection<Worker> Workers => _workers.AsReadOnly();

    public void AddWorker(Worker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);
        _workers.Add(worker);
    }

    public void RemoveWorker(Worker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);
        _workers.Remove(worker);
    }
}
