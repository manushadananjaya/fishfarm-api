using FishFarm.Domain.Common;
using FishFarm.Domain.Enums;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Junction record linking a Person to a FishFarm with a specific role (Position).
/// A single Person can have multiple FarmWorker records — one per farm they work at.
/// Soft-deleting this record removes the assignment without deleting the person.
/// </summary>
public sealed class FarmWorker : BaseAuditableEntity
{
    public Guid FishFarmId { get; set; }

    public Guid PersonId { get; set; }

    /// <summary>Role at THIS specific farm. A CEO at one farm can be a Worker at another.</summary>
    public WorkerPosition Position { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public FishFarm FishFarm { get; set; } = default!;
    public Person Person { get; set; } = default!;
}
