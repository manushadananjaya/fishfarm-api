using FishFarm.Domain.Common;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Represents an individual (person) who can be assigned to work at one or more fish farms.
/// Personal details (including the maritime CertifiedUntil date) are stored here once,
/// not duplicated per assignment.
/// </summary>
public sealed class Person : BaseAuditableEntity
{
    /// <summary>
    /// DB-generated sequential number. Display as "P-00001".
    /// Populated by SQL Server IDENTITY — never set manually.
    /// </summary>
    public int PersonNumber { get; private set; }

    public string Name { get; set; } = default!;

    /// <summary>Globally unique. Normalised to lower-case before persistence.</summary>
    public string Email { get; set; } = default!;

    public int Age { get; set; }

    /// <summary>
    /// Maritime certification expiry date — personal credential, not per-farm.
    /// An expired cert blocks working at any farm until renewed.
    /// </summary>
    public DateOnly CertifiedUntil { get; set; }

    public string? PictureUrl { get; set; }

    public string? PicturePublicId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly List<FarmWorker> _farmWorkers = [];
    public IReadOnlyCollection<FarmWorker> FarmWorkers => _farmWorkers.AsReadOnly();
}
