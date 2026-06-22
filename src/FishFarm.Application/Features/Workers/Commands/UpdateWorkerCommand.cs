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

        var req   = command.Request;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedEmail = req.Email.ToLowerInvariant();

        // CertifiedUntil must always be a strictly future date — setting it to today
        // or the past would immediately mark the worker as expired.
        // This mirrors the FluentValidation rule and acts as defense-in-depth for
        // non-HTTP callers (internal dispatch, tests, message consumers).
        if (req.CertifiedUntil <= today)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.CertifiedUntil)] =
                [
                    worker.CertifiedUntil < today
                        ? "This worker's certification has expired. " +
                          "Provide a future CertifiedUntil date to renew it before making other changes."
                        : "CertifiedUntil must be a future date."
                ]
            });

        if (!string.Equals(worker.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
            && await _uow.Workers.EmailExistsAsync(normalizedEmail, command.WorkerId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.Email)] = [$"Email '{req.Email}' is already in use."]
            });
        }

        // Guard: only one CEO per farm (allow keeping the same CEO position)
        if (req.Position == Domain.Enums.WorkerPosition.CEO
            && await _uow.Workers.HasCeoAsync(command.FishFarmId, command.WorkerId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.Position)] = ["This farm already has a CEO."]
            });

        worker.Name           = req.Name;
        worker.Age            = req.Age;
        worker.Email          = normalizedEmail;
        worker.Position       = req.Position;
        worker.CertifiedUntil = req.CertifiedUntil;

        _uow.Workers.Update(worker);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
