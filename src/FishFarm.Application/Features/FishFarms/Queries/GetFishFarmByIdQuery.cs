using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Application.Features.FishFarms.DTOs;
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
        var farm = await _uow.FishFarms.GetWithFarmWorkersAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), request.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new FishFarmDto
        {
            Id            = farm.Id,
            FarmCode      = $"FF-{farm.FarmNumber:D5}",
            Name          = farm.Name,
            GpsLatitude   = farm.GpsLatitude,
            GpsLongitude  = farm.GpsLongitude,
            NumberOfCages = farm.NumberOfCages,
            HasBarge      = farm.HasBarge,
            PictureUrl    = farm.PictureUrl,
            CreatedAt     = farm.CreatedAt,
            UpdatedAt     = farm.UpdatedAt,
            Workers       = farm.FarmWorkers
                .Select(fw => new FarmWorkerDto
                {
                    Id             = fw.Id,
                    FishFarmId     = fw.FishFarmId,
                    PersonId       = fw.PersonId,
                    PersonCode     = $"P-{fw.Person.PersonNumber:D5}",
                    PersonName     = fw.Person.Name,
                    PersonEmail    = fw.Person.Email,
                    PersonAge      = fw.Person.Age,
                    CertifiedUntil = fw.Person.CertifiedUntil,
                    IsExpired      = fw.Person.CertifiedUntil < today,
                    PictureUrl     = fw.Person.PictureUrl,
                    Position       = fw.Position.ToString(),
                    CreatedAt      = fw.CreatedAt,
                    UpdatedAt      = fw.UpdatedAt
                })
                .ToList()
        };
    }
}
