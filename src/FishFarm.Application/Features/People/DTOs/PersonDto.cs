namespace FishFarm.Application.Features.People.DTOs;

public sealed class PersonDto
{
    public Guid Id { get; init; }
    public string PersonCode { get; init; } = default!;
    public string Name { get; init; } = default!;   
    public string Email { get; init; } = default!;
    public int Age { get; init; }
    public DateOnly CertifiedUntil { get; init; }
    public bool IsExpired { get; init; }
    public string? PictureUrl { get; init; }
    public IReadOnlyList<PersonFarmAssignmentDto> Assignments { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class PersonSummaryDto
{
    public Guid Id { get; init; }
    public string PersonCode { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public int Age { get; init; }
    public DateOnly CertifiedUntil { get; init; }
    public bool IsExpired { get; init; }
    public string? PictureUrl { get; init; }
    public int FarmCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class PersonFarmAssignmentDto
{
    public Guid FarmWorkerId { get; init; }
    public Guid FishFarmId { get; init; }
    public string FarmName { get; init; } = default!;
    public string FarmCode { get; init; } = default!;
    public string Position { get; init; } = default!;
}
