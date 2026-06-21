using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

public sealed record CreateWorkerCommand(Guid FishFarmId, CreateWorkerRequest Request) : IRequest<Guid>;

public sealed class CreateWorkerCommandHandler : IRequestHandler<CreateWorkerCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public CreateWorkerCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task<Guid> Handle(CreateWorkerCommand command, CancellationToken cancellationToken)
    {
        // Verify parent farm exists
        _ = await _uow.FishFarms.GetByIdAsync(command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.FishFarmId);

        var req = command.Request;

        // Guard: email must be unique across all active workers
        if (await _uow.Workers.EmailExistsAsync(req.Email, cancellationToken: cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.Email)] = [$"Email '{req.Email}' is already in use."]
            });

        // Guard: only one CEO per farm
        if (req.Position == Domain.Enums.WorkerPosition.CEO
            && await _uow.Workers.HasCeoAsync(command.FishFarmId, cancellationToken: cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(req.Position)] = ["This farm already has a CEO."]
            });

        var worker = new Worker
        {
            FishFarmId     = command.FishFarmId,
            Name           = req.Name,
            Age            = req.Age,
            Email          = req.Email,
            Position       = req.Position,
            CertifiedUntil = req.CertifiedUntil
        };

        string? uploadedPublicId = null;
        if (req.Picture is not null)
        {
            var (url, publicId) = await _cloudinary.UploadImageAsync(
                req.Picture, "workers", cancellationToken);
            worker.PictureUrl      = url;
            worker.PicturePublicId = publicId;
            uploadedPublicId       = publicId;
        }

        await _uow.Workers.AddAsync(worker, cancellationToken);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // DB save failed — clean up the Cloudinary asset to avoid orphaned storage
            if (uploadedPublicId is not null)
                await _cloudinary.DeleteImageAsync(uploadedPublicId, CancellationToken.None);
            throw;
        }

        return worker.Id;
    }
}
