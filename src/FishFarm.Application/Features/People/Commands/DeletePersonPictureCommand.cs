using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Commands;

public sealed record DeletePersonPictureCommand(Guid PersonId) : IRequest;

public sealed class DeletePersonPictureCommandHandler : IRequestHandler<DeletePersonPictureCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public DeletePersonPictureCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeletePersonPictureCommand command, CancellationToken cancellationToken)
    {
        var person = await _uow.People.GetByIdAsync(command.PersonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Person), command.PersonId);

        var publicId = person.PicturePublicId;
        if (publicId is null) return;

        person.PictureUrl      = null;
        person.PicturePublicId = null;

        _uow.People.Update(person);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cloudinary.DeleteImageAsync(publicId, cancellationToken);
    }
}
