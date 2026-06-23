using FishFarm.Domain.Common;

namespace FishFarm.Domain.Entities;

public sealed class FishFarm : BaseAuditableEntity
{
    public int FarmNumber { get; private set; }

    public string Name { get; set; } = default!;

    public decimal GpsLatitude { get; set; }

    public decimal GpsLongitude { get; set; }

    public int NumberOfCages { get; set; }

    public bool HasBarge { get; set; }

    public string? PictureUrl { get; set; }

    public string? PicturePublicId { get; set; }

    private readonly List<FarmWorker> _farmWorkers = [];
    public IReadOnlyCollection<FarmWorker> FarmWorkers => _farmWorkers.AsReadOnly();

    public void AddFarmWorker(FarmWorker farmWorker)
    {
        ArgumentNullException.ThrowIfNull(farmWorker);
        _farmWorkers.Add(farmWorker);
    }
}
