using FishFarm.Domain.Common;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Aggregate root representing a fish farm.
/// Workers are no longer embedded here — use the FarmWorkers navigation for assignments.
/// </summary>
public sealed class FishFarm : BaseAuditableEntity
{
    /// <summary>
    /// DB-generated sequential number. Display as "FF-00001".
    /// Populated by SQL Server IDENTITY — never set manually.
    /// </summary>
    public int FarmNumber { get; private set; }

    public string Name { get; set; } = default!;

    public decimal GpsLatitude { get; set; }

    public decimal GpsLongitude { get; set; }

    public int NumberOfCages { get; set; }

    public bool HasBarge { get; set; }

    public string? PictureUrl { get; set; }

    public string? PicturePublicId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly List<FarmWorker> _farmWorkers = [];
    public IReadOnlyCollection<FarmWorker> FarmWorkers => _farmWorkers.AsReadOnly();

    /// <summary>Used by tests and seed data to attach an assignment without going through EF.</summary>
    public void AddFarmWorker(FarmWorker farmWorker)
    {
        ArgumentNullException.ThrowIfNull(farmWorker);
        _farmWorkers.Add(farmWorker);
    }
}
