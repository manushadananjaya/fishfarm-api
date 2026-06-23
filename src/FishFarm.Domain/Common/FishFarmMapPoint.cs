namespace FishFarm.Domain.Common;

/// <summary>
/// Lightweight projection returned by the repository for map-marker queries.
/// Contains only the data needed to place a pin on a map — no workers, no pictures.
/// Lives in Domain so IFishFarmRepository can reference it without creating a
/// circular dependency (Domain ← Application ← Infrastructure).
/// </summary>
public sealed record FishFarmMapPoint(
    Guid    Id,
    string  FarmCode,
    string  Name,
    decimal GpsLatitude,
    decimal GpsLongitude);
