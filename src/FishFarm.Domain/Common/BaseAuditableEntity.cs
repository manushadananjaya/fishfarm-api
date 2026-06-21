namespace FishFarm.Domain.Common;

/// <summary>
/// Base entity with full audit trail and soft-delete support.
/// All aggregate roots and entities should inherit from this class.
/// </summary>
public abstract class BaseAuditableEntity : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ── Audit ────────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ── Soft delete ──────────────────────────────────────────────────────────
    /// <summary>
    /// True when the entity has been logically deleted.
    /// Always filtered out by default EF Core queries.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp of logical deletion.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Identity (username / email) that performed the deletion.</summary>
    public string? DeletedBy { get; set; }
}
