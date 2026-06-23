using FishFarm.Application.Features.FarmWorkers.DTOs;

namespace FishFarm.Application.Features.FishFarms.DTOs;

public sealed class FishFarmDto
{
    public Guid Id { get; init; }

    public string FarmCode { get; init; } = default!;
    public string Name { get; init; } = default!;
    public decimal GpsLatitude { get; init; }
    public decimal GpsLongitude { get; init; }
    public int NumberOfCages { get; init; }
    public bool HasBarge { get; init; }
    public string? PictureUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<FarmWorkerDto> Workers { get; init; } = [];
}

public sealed class FishFarmSummaryDto
{
    public Guid Id { get; init; }
    public string FarmCode { get; init; } = default!;
    public string Name { get; init; } = default!;
    public decimal GpsLatitude { get; init; }
    public decimal GpsLongitude { get; init; }
    public int NumberOfCages { get; init; }
    public bool HasBarge { get; init; }
    public string? PictureUrl { get; init; }
    public int WorkerCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
