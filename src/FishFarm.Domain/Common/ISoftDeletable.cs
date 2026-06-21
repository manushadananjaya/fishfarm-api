namespace FishFarm.Domain.Common;

/// <summary>
/// Marks an entity as soft-deletable.
/// EF Core global query filters use this interface to automatically
/// exclude logically deleted rows from all queries.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
}
