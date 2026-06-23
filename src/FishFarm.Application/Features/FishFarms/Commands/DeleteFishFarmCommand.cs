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


        foreach (var assignment in farm.FarmWorkers)
            _uow.FarmWorkers.Delete(assignment);

        _uow.FishFarms.Delete(farm);

      
        await _uow.SaveChangesAsync(cancellationToken);

       
        await _cloudinary.DeleteImageAsync(farm.PicturePublicId, cancellationToken);
    }
}
