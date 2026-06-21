using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

public sealed record UpdateWorkerCommand(Guid FishFarmId, Guid WorkerId, UpdateWorkerRequest Request)
    : IRequest;

public sealed class UpdateWorkerCommandHandler : IRequestHandler<UpdateWorkerCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateWorkerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateWorkerCommand command, CancellationToken cancellationToken)
    {
        var worker = await _uow.Workers.GetByIdAndFarmAsync(
            command.WorkerId, command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worker), command.WorkerId);

        var req = command.Request;
        worker.Name           = req.Name;
        worker.Age            = req.Age;
        worker.Email          = req.Email;
        worker.Position       = req.Position;
        worker.CertifiedUntil = req.CertifiedUntil;

        _uow.Workers.Update(worker);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
