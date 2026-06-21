using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Queries;

public sealed record GetFishFarmByIdQuery(Guid Id) : IRequest<FishFarmDto>;

public sealed class GetFishFarmByIdQueryHandler
    : IRequestHandler<GetFishFarmByIdQuery, FishFarmDto>
{
    private readonly IUnitOfWork _uow;

    public GetFishFarmByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<FishFarmDto> Handle(
        GetFishFarmByIdQuery request,
        CancellationToken cancellationToken)
    {
        var farm = await _uow.FishFarms.GetWithWorkersAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), request.Id);

        return new FishFarmDto
        {
            Id            = farm.Id,
            Name          = farm.Name,
            GpsLatitude   = farm.GpsLatitude,
            GpsLongitude  = farm.GpsLongitude,
            NumberOfCages = farm.NumberOfCages,
            HasBarge      = farm.HasBarge,
            PictureUrl    = farm.PictureUrl,
            CreatedAt     = farm.CreatedAt,
            UpdatedAt     = farm.UpdatedAt,
            Workers       = farm.Workers
                .Where(w => !w.IsDeleted)
                .Select(w => new WorkerDto
                {
                    Id             = w.Id,
                    FishFarmId     = w.FishFarmId,
                    Name           = w.Name,
                    Age            = w.Age,
                    Email          = w.Email,
                    Position       = w.Position.ToString(),
                    CertifiedUntil = w.CertifiedUntil,
                    PictureUrl     = w.PictureUrl,
                    CreatedAt      = w.CreatedAt,
                    UpdatedAt      = w.UpdatedAt
                })
                .ToList()
        };
    }
}
