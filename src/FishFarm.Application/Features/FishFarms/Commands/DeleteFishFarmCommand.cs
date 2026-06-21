using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Commands;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record DeleteFishFarmCommand(Guid Id) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class DeleteFishFarmCommandHandler : IRequestHandler<DeleteFishFarmCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public DeleteFishFarmCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeleteFishFarmCommand command, CancellationToken cancellationToken)
    {
        var farm = await _uow.FishFarms.GetWithWorkersAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.Id);

        // Soft-delete all workers belonging to this farm (cascade soft-delete)
        foreach (var worker in farm.Workers.Where(w => !w.IsDeleted))
        {
            _uow.Workers.Delete(worker);
            // Clean up worker Cloudinary assets
            await _cloudinary.DeleteImageAsync(worker.PicturePublicId, cancellationToken);
        }

        // Soft-delete the farm itself
        _uow.FishFarms.Delete(farm);

        // Delete farm Cloudinary image
        await _cloudinary.DeleteImageAsync(farm.PicturePublicId, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
