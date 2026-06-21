using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Commands;

public sealed record UpdateWorkerPictureCommand(
    Guid FishFarmId,
    Guid WorkerId,
    UpdateWorkerPictureRequest Request) : IRequest<string>;

public sealed class UpdateWorkerPictureCommandHandler
    : IRequestHandler<UpdateWorkerPictureCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public UpdateWorkerPictureCommandHandler(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow       = uow;
        _cloudinary = cloudinary;
    }

    public async Task<string> Handle(
        UpdateWorkerPictureCommand command,
        CancellationToken cancellationToken)
    {
        var worker = await _uow.Workers.GetByIdAndFarmAsync(
            command.WorkerId, command.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worker), command.WorkerId);

        await _cloudinary.DeleteImageAsync(worker.PicturePublicId, cancellationToken);

        var (url, publicId) = await _cloudinary.UploadImageAsync(
            command.Request.Picture, "workers", cancellationToken);

        worker.PictureUrl      = url;
        worker.PicturePublicId = publicId;

        _uow.Workers.Update(worker);
        await _uow.SaveChangesAsync(cancellationToken);

        return url;
    }
}
