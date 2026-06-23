namespace FishFarm.Application.Features.FarmWorkers.DTOs;

/// <summary>Represents one person's assignment at a specific farm.</summary>
public sealed class FarmWorkerDto
{
    /// <summary>The FarmWorker record ID (use this to update/remove the assignment).</summary>
    public Guid Id { get; init; }
    public Guid FishFarmId { get; init; }
    public Guid PersonId { get; init; }
    /// <summary>Human-readable person code, e.g. "P-00001".</summary>
    public string PersonCode { get; init; } = default!;
    public string PersonName { get; init; } = default!;
    public string PersonEmail { get; init; } = default!;
    public int PersonAge { get; init; }
    public DateOnly CertifiedUntil { get; init; }
    public bool IsExpired { get; init; }
    public string? PictureUrl { get; init; }
    /// <summary>Role at this specific farm.</summary>
    public string Position { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
