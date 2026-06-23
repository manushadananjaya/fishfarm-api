namespace FishFarm.Application.Features.FishFarms.DTOs;

/// <summary>
/// Farm detail DTO returned by the map endpoint.
/// Includes GPS coordinates, core farm properties, and the active worker count —
/// enough for a map marker and a summary info panel without a second round-trip.
/// </summary>
public sealed class FishFarmMapDto
{
    /// <summary>Stable GUID — use as the map marker key.</summary>
    public Guid Id { get; init; }

    /// <summary>Human-readable display identifier, e.g. "FF-00001".</summary>
    public string FarmCode { get; init; } = default!;

    public string Name { get; init; } = default!;
    public decimal GpsLatitude { get; init; }
    public decimal GpsLongitude { get; init; }
    public int NumberOfCages { get; init; }
    public bool HasBarge { get; init; }
    public string? PictureUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>Number of active worker assignments at this farm.</summary>
    public int WorkerCount { get; init; }
}
