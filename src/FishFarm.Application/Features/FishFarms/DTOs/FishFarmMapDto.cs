namespace FishFarm.Application.Features.FishFarms.DTOs;

/// <summary>
/// Lightweight DTO used exclusively by the map endpoint.
/// Contains only the fields required to render a map marker.
/// Deliberately excludes workers, pictures, cage counts, audit fields, etc.
/// </summary>
public sealed class FishFarmMapDto
{
    /// <summary>Stable GUID — use this as the map marker key and for deep-link navigation.</summary>
    public Guid Id { get; init; }

    /// <summary>Human-readable display identifier, e.g. "FF-00001".</summary>
    public string FarmCode { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal GpsLatitude { get; init; }

    public decimal GpsLongitude { get; init; }
}
