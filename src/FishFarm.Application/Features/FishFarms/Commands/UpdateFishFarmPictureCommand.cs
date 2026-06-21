using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Commands;

public sealed record UpdateFishFarmPictureCommand(Guid Id, UpdateFishFarmPictureRequest Request)
    : IRequest<string>;

public sealed class UpdateFishFarmPictureCommandHandler
    : IRequestHandler<UpdateFishFarmPictureCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public UpdateFishFarmPictureCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task<string> Handle(
        UpdateFishFarmPictureCommand command,
        CancellationToken cancellationToken)
    {
        var farm = await _uow.FishFarms.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.Id);

        // Delete old image from Cloudinary if it exists
        await _cloudinary.DeleteImageAsync(farm.PicturePublicId, cancellationToken);

        // Upload new image
        var (url, publicId) = await _cloudinary.UploadImageAsync(
            command.Request.Picture, "fishfarms", cancellationToken);

        farm.PictureUrl      = url;
        farm.PicturePublicId = publicId;

        _uow.FishFarms.Update(farm);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // DB save failed — clean up the newly uploaded Cloudinary asset.
            // Note: the old image is already gone at this point; that is an accepted trade-off.
            await _cloudinary.DeleteImageAsync(publicId, CancellationToken.None);
            throw;
        }

        return url;
    }
}
