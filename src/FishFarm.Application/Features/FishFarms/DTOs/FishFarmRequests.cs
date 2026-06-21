using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Features.FishFarms.DTOs;

public sealed class CreateFishFarmRequest
{
    public string Name { get; init; } = default!;
    public decimal GpsLatitude { get; init; }
    public decimal GpsLongitude { get; init; }
    public int NumberOfCages { get; init; }
    public bool HasBarge { get; init; }

    // Design decision (#4): Picture is intentionally optional on creation.
    // A farm can be registered first, then its picture uploaded via PATCH /picture.
    // This avoids blocking farm creation on image availability and is consistent
    // with how worker pictures are handled. The PATCH endpoint always requires a file.
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
