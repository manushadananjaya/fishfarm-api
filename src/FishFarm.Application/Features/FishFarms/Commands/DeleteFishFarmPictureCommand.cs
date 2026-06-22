using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Commands;

public sealed record DeleteFishFarmPictureCommand(Guid Id) : IRequest;

public sealed class DeleteFishFarmPictureCommandHandler : IRequestHandler<DeleteFishFarmPictureCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public DeleteFishFarmPictureCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeleteFishFarmPictureCommand command, CancellationToken cancellationToken)
    {
        var farm = await _uow.FishFarms.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.Id);

        // Idempotent — nothing to do if the farm has no picture
        if (farm.PicturePublicId is null)
            return;

        var publicId = farm.PicturePublicId;

        farm.PictureUrl      = null;
        farm.PicturePublicId = null;

        _uow.FishFarms.Update(farm);

        // DB commit first — CDN asset is unreachable once the URL is removed from the record.
        await _uow.SaveChangesAsync(cancellationToken);

        // Best-effort CDN cleanup after commit.
        await _cloudinary.DeleteImageAsync(publicId, cancellationToken);
    }
}
