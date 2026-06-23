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
        var farm = await _uow.FishFarms.GetWithFarmWorkersAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.Id);

        // Soft-delete all active assignments at this farm.
        // The people themselves are NOT deleted — they may work at other farms.
        // Global query filter means farm.FarmWorkers already excludes soft-deleted entries.
        foreach (var assignment in farm.FarmWorkers)
            _uow.FarmWorkers.Delete(assignment);

        _uow.FishFarms.Delete(farm);

        // DB commit is the source of truth — CDN cleanup runs only after success.
        await _uow.SaveChangesAsync(cancellationToken);

        // Delete farm picture from Cloudinary (best-effort).
        await _cloudinary.DeleteImageAsync(farm.PicturePublicId, cancellationToken);
    }
}
