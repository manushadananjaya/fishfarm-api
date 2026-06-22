using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

public sealed record DeleteWorkerPictureCommand(Guid FishFarmId, Guid WorkerId) : IRequest;

public sealed class DeleteWorkerPictureCommandHandler : IRequestHandler<DeleteWorkerPictureCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public DeleteWorkerPictureCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeleteWorkerPictureCommand command, CancellationToken cancellationToken)
    {
        var worker = await _uow.Workers.GetByIdAndFarmAsync(
            command.WorkerId, command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worker), command.WorkerId);

        // Idempotent — nothing to do if the worker has no picture
        if (worker.PicturePublicId is null)
            return;

        var publicId = worker.PicturePublicId;

        worker.PictureUrl      = null;
        worker.PicturePublicId = null;

        _uow.Workers.Update(worker);

        // DB commit first — CDN asset is unreachable once the URL is removed from the record.
        await _uow.SaveChangesAsync(cancellationToken);

        // Best-effort CDN cleanup after commit.
        await _cloudinary.DeleteImageAsync(publicId, cancellationToken);
    }
}
