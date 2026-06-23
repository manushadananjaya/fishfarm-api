using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Commands;

public sealed record CreatePersonCommand(CreatePersonRequest Request) : IRequest<Guid>;

public sealed class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public CreatePersonCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task<Guid> Handle(CreatePersonCommand command, CancellationToken cancellationToken)
    {
        var req             = command.Request;
        var normalizedEmail = req.Email.ToLowerInvariant();

        if (await _uow.People.EmailExistsAsync(normalizedEmail, cancellationToken: cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.Email)] = [$"Email '{req.Email}' is already in use."]
            });

        var person = new Person
        {
            Name           = req.Name,
            Email          = normalizedEmail,
            Age            = req.Age,
            CertifiedUntil = req.CertifiedUntil
        };

        string? uploadedPublicId = null;
        if (req.Picture is not null)
        {
            var (url, publicId) = await _cloudinary.UploadImageAsync(
                req.Picture, "people", cancellationToken);
            person.PictureUrl      = url;
            person.PicturePublicId = publicId;
            uploadedPublicId       = publicId;
        }

        await _uow.People.AddAsync(person, cancellationToken);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (uploadedPublicId is not null)
                await _cloudinary.DeleteImageAsync(uploadedPublicId, CancellationToken.None);
            throw;
        }

        return person.Id;
    }
}
