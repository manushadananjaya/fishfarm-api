using FishFarm.Domain.Common;

namespace FishFarm.Domain.Entities;

public sealed class Person : BaseAuditableEntity
{
    public int PersonNumber { get; private set; }

    public string Name { get; set; } = default!;

    public string Email { get; set; } = default!;

    public int Age { get; set; }

    public DateOnly CertifiedUntil { get; set; }

    public string? PictureUrl { get; set; }

    public string? PicturePublicId { get; set; }

    private readonly List<FarmWorker> _farmWorkers = [];
    public IReadOnlyCollection<FarmWorker> FarmWorkers => _farmWorkers.AsReadOnly();
}
