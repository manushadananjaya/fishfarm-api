using FishFarm.Domain.Enums;

namespace FishFarm.Application.Features.FarmWorkers.DTOs;

/// <summary>Assign an existing person to a farm.</summary>
public sealed class AssignPersonToFarmRequest
{
    public Guid PersonId { get; init; }
    public WorkerPosition Position { get; init; }
}

/// <summary>Change the position of an existing assignment.</summary>
public sealed class UpdateFarmWorkerRequest
{
    public WorkerPosition Position { get; init; }
}
