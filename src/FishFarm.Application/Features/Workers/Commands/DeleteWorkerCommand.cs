using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

public sealed record DeleteWorkerCommand(Guid FishFarmId, Guid WorkerId) : IRequest;

public sealed class DeleteWorkerCommandHandler : IRequestHandler<DeleteWorkerCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public DeleteWorkerCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeleteWorkerCommand command, CancellationToken cancellationToken)
    {
        var worker = await _uow.Workers.GetByIdAndFarmAsync(
            command.WorkerId, command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worker), command.WorkerId);

        // Capture the public ID before mutating state.
        var picturePublicId = worker.PicturePublicId;

        _uow.Workers.Delete(worker);

        // DB commit is the source of truth — if this fails, no CDN asset is touched.
        await _uow.SaveChangesAsync(cancellationToken);

        // CDN cleanup is best-effort after the DB record is committed.
        await _cloudinary.DeleteImageAsync(picturePublicId, cancellationToken);
    }
}
