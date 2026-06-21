using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Features.FishFarms.Commands;

public sealed record CreateFishFarmCommand(CreateFishFarmRequest Request) : IRequest<Guid>;

public sealed class CreateFishFarmCommandHandler
    : IRequestHandler<CreateFishFarmCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public CreateFishFarmCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task<Guid> Handle(
        CreateFishFarmCommand command,
        CancellationToken cancellationToken)
    {
        var req = command.Request;

        var farm = new Domain.Entities.FishFarm
        {
            Name          = req.Name,
            GpsLatitude   = req.GpsLatitude,
            GpsLongitude  = req.GpsLongitude,
            NumberOfCages = req.NumberOfCages,
            HasBarge      = req.HasBarge
        };

        // Upload picture if provided — must happen before DB save so we have the URL
        string? uploadedPublicId = null;
        if (req.Picture is not null)
        {
            var (url, publicId) = await _cloudinary.UploadImageAsync(
                req.Picture, "fishfarms", cancellationToken);
            farm.PictureUrl      = url;
            farm.PicturePublicId = publicId;
            uploadedPublicId     = publicId;
        }

        await _uow.FishFarms.AddAsync(farm, cancellationToken);

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

        return farm.Id;
    }
}
