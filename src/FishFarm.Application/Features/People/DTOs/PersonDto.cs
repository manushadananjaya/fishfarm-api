namespace FishFarm.Application.Features.People.DTOs;

public sealed class PersonDto
{
    public Guid Id { get; init; }
    /// <summary>Human-readable display ID, e.g. "P-00001".</summary>
    public string PersonCode { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public int Age { get; init; }
    public DateOnly CertifiedUntil { get; init; }
    public bool IsExpired { get; init; }
    public string? PictureUrl { get; init; }
    /// <summary>Active farm assignments for this person.</summary>
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
    /// <summary>Number of active farm assignments.</summary>
    public int FarmCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Compact farm assignment embedded inside PersonDto.</summary>
public sealed class PersonFarmAssignmentDto
{
    public Guid FarmWorkerId { get; init; }
    public Guid FishFarmId { get; init; }
    public string FarmName { get; init; } = default!;
    public string FarmCode { get; init; } = default!;
    public string Position { get; init; } = default!;
}
