using FishFarm.Domain.Common;
using FishFarm.Domain.Enums;

namespace FishFarm.Domain.Entities;

/// <summary>
/// Represents a worker assigned to a fish farm.
/// </summary>
public sealed class Worker : BaseAuditableEntity
{
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
