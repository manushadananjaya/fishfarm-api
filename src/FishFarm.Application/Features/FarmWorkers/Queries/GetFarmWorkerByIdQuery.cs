using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FarmWorkers.Queries;

public sealed record GetFarmWorkerByIdQuery(Guid FishFarmId, Guid FarmWorkerId)
    : IRequest<FarmWorkerDto>;

public sealed class GetFarmWorkerByIdQueryHandler
    : IRequestHandler<GetFarmWorkerByIdQuery, FarmWorkerDto>
{
    private readonly IUnitOfWork _uow;
    public GetFarmWorkerByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<FarmWorkerDto> Handle(
        GetFarmWorkerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var fw = await _uow.FarmWorkers.GetByIdAndFarmAsync(
            request.FarmWorkerId, request.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FarmWorker), request.FarmWorkerId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new FarmWorkerDto
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
        };
    }
}
