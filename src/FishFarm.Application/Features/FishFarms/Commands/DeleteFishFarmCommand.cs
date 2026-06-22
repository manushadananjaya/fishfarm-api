using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Commands;

public sealed record DeleteFishFarmCommand(Guid Id) : IRequest;

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

        // Collect Cloudinary public IDs before mutating any state.
        // Global query filter means farm.Workers already excludes soft-deleted entries.
        var workerPublicIds = farm.Workers
            .Select(w => w.PicturePublicId)
            .Where(id => id is not null)
            .ToList();

        // Apply all soft-deletes in memory (no I/O), then persist atomically in one round-trip.
        foreach (var worker in farm.Workers)
            _uow.Workers.Delete(worker);

        _uow.FishFarms.Delete(farm);

        // DB commit is the source of truth — if this fails, no assets are touched.
        await _uow.SaveChangesAsync(cancellationToken);

        // CDN cleanup is best-effort after the DB is committed. Run in parallel to avoid N+1 latency.
        // Orphaned assets on partial failure are recoverable via monitoring; they are not user-visible.
        var cloudinaryTasks = workerPublicIds
            .Select(id => _cloudinary.DeleteImageAsync(id!, cancellationToken))
            .Append(_cloudinary.DeleteImageAsync(farm.PicturePublicId, cancellationToken));

        await Task.WhenAll(cloudinaryTasks);
    }
}
