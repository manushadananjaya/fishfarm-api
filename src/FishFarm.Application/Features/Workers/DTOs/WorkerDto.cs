using FishFarm.Domain.Enums;

namespace FishFarm.Application.Features.Workers.DTOs;

public sealed class WorkerDto
{
    public Guid Id { get; init; }
    public Guid FishFarmId { get; init; }
    public string Name { get; init; } = default!;
    public int Age { get; init; }
    public string Email { get; init; } = default!;

    /// <summary>
    /// Enum integer value is stored in DB; returned as string name in API responses
    /// so clients don't need to know the mapping.
    /// </summary>
    public string Position { get; init; } = default!;

    public DateOnly CertifiedUntil { get; init; }
    public string? PictureUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
