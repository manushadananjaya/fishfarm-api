using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Commands;

public sealed record DeletePersonCommand(Guid PersonId) : IRequest;

public sealed class DeletePersonCommandHandler : IRequestHandler<DeletePersonCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public DeletePersonCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeletePersonCommand command, CancellationToken cancellationToken)
    {
        var person = await _uow.People.GetByIdWithAssignmentsAsync(
            command.PersonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Person), command.PersonId);

        var picturePublicId = person.PicturePublicId;

        foreach (var assignment in person.FarmWorkers)
            _uow.FarmWorkers.Delete(assignment);

        _uow.People.Delete(person);

        await _uow.SaveChangesAsync(cancellationToken);

        await _cloudinary.DeleteImageAsync(picturePublicId, cancellationToken);
    }
}
