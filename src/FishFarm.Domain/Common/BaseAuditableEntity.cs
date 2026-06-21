namespace FishFarm.Domain.Common;

/// <summary>
/// Base entity with full audit trail and soft-delete support.
/// All aggregate roots and entities should inherit from this class.
/// </summary>
public abstract class BaseAuditableEntity : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
