using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FarmWorkers.Commands;

public sealed record AssignPersonToFarmCommand(
    Guid FishFarmId,
    AssignPersonToFarmRequest Request) : IRequest<Guid>;

public sealed class AssignPersonToFarmCommandHandler
    : IRequestHandler<AssignPersonToFarmCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public AssignPersonToFarmCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(
        AssignPersonToFarmCommand command,
        CancellationToken cancellationToken)
    {
        _ = await _uow.FishFarms.GetByIdAsync(command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.FishFarmId);

        var person = await _uow.People.GetByIdAsync(command.Request.PersonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Person), command.Request.PersonId);

        // Guard: person can only be assigned once per farm (active)
        if (await _uow.FarmWorkers.IsPersonAssignedAsync(
                command.FishFarmId, command.Request.PersonId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Request.PersonId)] =
                    [$"Person '{person.Name}' is already assigned to this farm."]
            });
        }

        // Guard: only one CEO per farm
        if (command.Request.Position == Domain.Enums.WorkerPosition.CEO
            && await _uow.FarmWorkers.HasCeoAsync(command.FishFarmId, cancellationToken: cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Request.Position)] = ["This farm already has a CEO."]
            });
        }

        var farmWorker = new FarmWorker
        {
            FishFarmId = command.FishFarmId,
            PersonId   = command.Request.PersonId,
            Position   = command.Request.Position
        };

        await _uow.FarmWorkers.AddAsync(farmWorker, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return farmWorker.Id;
    }
}
