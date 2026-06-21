using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Commands;

public sealed record UpdateFishFarmCommand(Guid Id, UpdateFishFarmRequest Request) : IRequest;

public sealed class UpdateFishFarmCommandHandler : IRequestHandler<UpdateFishFarmCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateFishFarmCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateFishFarmCommand command, CancellationToken cancellationToken)
    {
        var farm = await _uow.FishFarms.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), command.Id);

        var req = command.Request;
        farm.Name          = req.Name;
        farm.GpsLatitude   = req.GpsLatitude;
        farm.GpsLongitude  = req.GpsLongitude;
        farm.NumberOfCages = req.NumberOfCages;
        farm.HasBarge      = req.HasBarge;

        _uow.FishFarms.Update(farm);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
