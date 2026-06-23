namespace FishFarm.Domain.Common;


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
