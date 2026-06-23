namespace FishFarm.Application.Features.FarmWorkers.DTOs;


public sealed class FarmWorkerDto
{
    
    public Guid Id { get; init; }
    public Guid FishFarmId { get; init; }
    public Guid PersonId { get; init; }
    public string PersonCode { get; init; } = default!;
    public string PersonName { get; init; } = default!;
    public string PersonEmail { get; init; } = default!;
    public int PersonAge { get; init; }
    public DateOnly CertifiedUntil { get; init; }
    public bool IsExpired { get; init; }
    public string? PictureUrl { get; init; }
    public string Position { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
