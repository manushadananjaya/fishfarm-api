using FishFarm.Domain.Enums;

namespace FishFarm.Application.Features.FarmWorkers.DTOs;


public sealed class AssignPersonToFarmRequest
{
    public Guid PersonId { get; init; }
    public WorkerPosition Position { get; init; }
}

public sealed class UpdateFarmWorkerRequest
{
    public WorkerPosition Position { get; init; }
}
