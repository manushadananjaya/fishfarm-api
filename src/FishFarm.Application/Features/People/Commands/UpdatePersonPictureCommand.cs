using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Commands;

public sealed record UpdatePersonPictureCommand(Guid PersonId, UpdatePersonPictureRequest Request)
    : IRequest<string>;

public sealed class UpdatePersonPictureCommandHandler
    : IRequestHandler<UpdatePersonPictureCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public UpdatePersonPictureCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task<string> Handle(
        UpdatePersonPictureCommand command,
        CancellationToken cancellationToken)
    {
        var person = await _uow.People.GetByIdAsync(command.PersonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Person), command.PersonId);

        await _cloudinary.DeleteImageAsync(person.PicturePublicId, cancellationToken);

        var (url, publicId) = await _cloudinary.UploadImageAsync(
            command.Request.Picture, "people", cancellationToken);

        person.PictureUrl      = url;
        person.PicturePublicId = publicId;

        _uow.People.Update(person);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _cloudinary.DeleteImageAsync(publicId, CancellationToken.None);
            throw;
        }

        return url;
    }
}
