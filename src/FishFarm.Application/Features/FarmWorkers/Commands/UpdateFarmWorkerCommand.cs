using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FarmWorkers.Commands;

public sealed record UpdateFarmWorkerCommand(
    Guid FishFarmId,
    Guid FarmWorkerId,
    UpdateFarmWorkerRequest Request) : IRequest;

public sealed class UpdateFarmWorkerCommandHandler : IRequestHandler<UpdateFarmWorkerCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateFarmWorkerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateFarmWorkerCommand command, CancellationToken cancellationToken)
    {
        var fw = await _uow.FarmWorkers.GetByIdAndFarmAsync(
            command.FarmWorkerId, command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.FarmWorker), command.FarmWorkerId);

        if (command.Request.Position == Domain.Enums.WorkerPosition.CEO
            && await _uow.FarmWorkers.HasCeoAsync(
                command.FishFarmId, command.FarmWorkerId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Request.Position)] = ["This farm already has a CEO."]
            });
        }

        fw.Position = command.Request.Position;

        _uow.FarmWorkers.Update(fw);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
