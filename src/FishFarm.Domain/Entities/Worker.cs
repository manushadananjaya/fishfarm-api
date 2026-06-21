using FishFarm.Domain.Common;
using FishFarm.Domain.Enums;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Represents a worker assigned to a fish farm.
/// </summary>
public sealed class Worker : BaseAuditableEntity
{
    // ── Core properties ──────────────────────────────────────────────────────

    /// <summary>Foreign key to the parent FishFarm.</summary>
    public Guid FishFarmId { get; set; }

    public string Name { get; set; } = default!;

    public int Age { get; set; }

    public string Email { get; set; } = default!;

    /// <summary>
    /// Stored as INT in the database.
    /// Mapped via EF HasConversion&lt;int&gt;() in WorkerConfiguration.
    /// </summary>
    public WorkerPosition Position { get; set; }

    /// <summary>Date until which the worker's certification is valid.</summary>
    public DateOnly CertifiedUntil { get; set; }

    // ── Image (Cloudinary) ───────────────────────────────────────────────────

    /// <summary>Optional worker profile picture URL.</summary>
    public string? PictureUrl { get; set; }

    /// <summary>Cloudinary public_id for deletion / replacement.</summary>
    public string? PicturePublicId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────

    public FishFarm FishFarm { get; set; } = default!;
}
