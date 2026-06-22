using FishFarm.Domain.Enums;

namespace FishFarm.Application.Features.Workers.DTOs;

public sealed class WorkerDto
{
    public Guid Id { get; init; }
    /// <summary>Human-readable display ID, e.g. "WK-00001".</summary>
    public string WorkerCode { get; init; } = default!;
    public Guid FishFarmId { get; init; }
    public string Name { get; init; } = default!;
    public int Age { get; init; }
    public string Email { get; init; } = default!;
    public string Position { get; init; } = default!;
    public DateOnly CertifiedUntil { get; init; }
    public bool IsExpired { get; init; }
    public string? PictureUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
