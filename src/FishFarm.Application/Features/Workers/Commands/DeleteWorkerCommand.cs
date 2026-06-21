using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record DeleteWorkerCommand(Guid FishFarmId, Guid WorkerId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

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

        _uow.Workers.Delete(worker);
        await _cloudinary.DeleteImageAsync(worker.PicturePublicId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
