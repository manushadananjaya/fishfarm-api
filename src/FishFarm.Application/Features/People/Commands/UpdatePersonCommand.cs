using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Commands;

public sealed record UpdatePersonCommand(Guid PersonId, UpdatePersonRequest Request) : IRequest;

public sealed class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdatePersonCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdatePersonCommand command, CancellationToken cancellationToken)
    {
        var person = await _uow.People.GetByIdAsync(command.PersonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Person), command.PersonId);

        var req             = command.Request;
        var today           = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedEmail = req.Email.ToLowerInvariant();

        if (req.CertifiedUntil <= today)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.CertifiedUntil)] =
                [
                    person.CertifiedUntil < today
                        ? "This person's maritime certification has expired. " +
                          "Provide a future CertifiedUntil date to renew it."
                        : "CertifiedUntil must be a future date."
                ]
            });

        if (!string.Equals(person.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
            && await _uow.People.EmailExistsAsync(normalizedEmail, command.PersonId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.Email)] = [$"Email '{req.Email}' is already in use."]
            });
        }

        person.Name           = req.Name;
        person.Email          = normalizedEmail;
        person.Age            = req.Age;
        person.CertifiedUntil = req.CertifiedUntil;

        _uow.People.Update(person);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
