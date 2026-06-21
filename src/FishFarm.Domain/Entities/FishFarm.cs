using FishFarm.Domain.Common;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Aggregate root representing a fish farm.
/// </summary>
public sealed class FishFarm : BaseAuditableEntity
{
    // ── Core properties ──────────────────────────────────────────────────────

    /// <summary>Display name of the fish farm.</summary>
    public string Name { get; set; } = default!;

    /// <summary>GPS latitude with 4 decimal precision (e.g. 60.3913).</summary>
    public decimal GpsLatitude { get; set; }

    /// <summary>GPS longitude with 4 decimal precision (e.g. 5.3221).</summary>
    public decimal GpsLongitude { get; set; }

    /// <summary>Total number of fish cages at this farm.</summary>
    public int NumberOfCages { get; set; }

    /// <summary>Indicates whether the farm has a barge.</summary>
    public bool HasBarge { get; set; }

    // ── Image (Cloudinary) ───────────────────────────────────────────────────

    /// <summary>Publicly accessible Cloudinary URL of the farm picture.</summary>
    public string? PictureUrl { get; set; }

    /// <summary>Cloudinary public_id used for deletion / replacement.</summary>
    public string? PicturePublicId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────

    private readonly List<Worker> _workers = [];

    /// <summary>Workers assigned to this farm (read-only collection).</summary>
    public IReadOnlyCollection<Worker> Workers => _workers.AsReadOnly();

    // ── Domain methods ───────────────────────────────────────────────────────

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
