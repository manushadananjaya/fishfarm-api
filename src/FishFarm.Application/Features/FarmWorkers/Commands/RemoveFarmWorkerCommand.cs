using FishFarm.Application.Common.Exceptions;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FarmWorkers.Commands;

/// <summary>Soft-deletes a person's assignment at a farm. Does not delete the person.</summary>
public sealed record RemoveFarmWorkerCommand(Guid FishFarmId, Guid FarmWorkerId) : IRequest;

public sealed class RemoveFarmWorkerCommandHandler : IRequestHandler<RemoveFarmWorkerCommand>
{
    private readonly IUnitOfWork _uow;
    public RemoveFarmWorkerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(RemoveFarmWorkerCommand command, CancellationToken cancellationToken)
    {
        var fw = await _uow.FarmWorkers.GetByIdAndFarmAsync(
            command.FarmWorkerId, command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.FarmWorker), command.FarmWorkerId);

        _uow.FarmWorkers.Delete(fw);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
