using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Features.FishFarms.DTOs;

public sealed class CreateFishFarmRequest
{
    public string Name { get; init; } = default!;
    public decimal GpsLatitude { get; init; }
    public decimal GpsLongitude { get; init; }
    public int NumberOfCages { get; init; }
    public bool HasBarge { get; init; }

    /// <summary>Optional picture uploaded via multipart/form-data.</summary>
    public IFormFile? Picture { get; init; }
}

public sealed class UpdateFishFarmRequest
{
    public string Name { get; init; } = default!;
    public decimal GpsLatitude { get; init; }
    public decimal GpsLongitude { get; init; }
    public int NumberOfCages { get; init; }
    public bool HasBarge { get; init; }
}

public sealed class UpdateFishFarmPictureRequest
{
    public IFormFile Picture { get; init; } = default!;
}
