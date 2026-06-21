using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record CreateWorkerCommand(Guid FishFarmId, CreateWorkerRequest Request) : IRequest<Guid>;

// ── Handler ──────────────────────────────────────────────────────────────────

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

        var worker = new Worker
        {
            FishFarmId     = command.FishFarmId,
            Name           = req.Name,
            Age            = req.Age,
            Email          = req.Email,
            Position       = req.Position,
            CertifiedUntil = req.CertifiedUntil
        };

        if (req.Picture is not null)
        {
            var (url, publicId) = await _cloudinary.UploadImageAsync(
                req.Picture, "workers", cancellationToken);
            worker.PictureUrl      = url;
            worker.PicturePublicId = publicId;
        }

        await _uow.Workers.AddAsync(worker, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return worker.Id;
    }
}
