using FishFarm.Domain.Common;
using FishFarm.Domain.Enums;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Represents a worker assigned to a fish farm.
/// </summary>
public sealed class Worker : BaseAuditableEntity
{
    /// <summary>
    /// DB-generated sequential number used as the human-readable display identifier.
    /// Populated by SQL Server IDENTITY after the first SaveChanges.
    /// Do not set this manually — use the formatted <c>WorkerCode</c> ("WK-00001") in DTOs.
    /// </summary>
    public int WorkerNumber { get; private set; }

    public Guid FishFarmId { get; set; }

    public string Name { get; set; } = default!;

    public int Age { get; set; }

    public string Email { get; set; } = default!;

    public WorkerPosition Position { get; set; }

    public DateOnly CertifiedUntil { get; set; }

    public string? PictureUrl { get; set; }

    public string? PicturePublicId { get; set; }

    public FishFarm FishFarm { get; set; } = default!;
}
