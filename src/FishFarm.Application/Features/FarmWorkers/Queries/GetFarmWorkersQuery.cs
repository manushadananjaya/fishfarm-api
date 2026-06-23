using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FarmWorkers.Queries;

public sealed record GetFarmWorkersQuery(
    Guid            FishFarmId,
    int             PageNumber  = 1,
    int             PageSize    = 20,
    string?         Search      = null,
    WorkerPosition? Position    = null,
    bool?           CertExpired = null) : IRequest<PaginatedResult<FarmWorkerDto>>;

public sealed class GetFarmWorkersQueryHandler
    : IRequestHandler<GetFarmWorkersQuery, PaginatedResult<FarmWorkerDto>>
{
    private readonly IUnitOfWork _uow;
    public GetFarmWorkersQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedResult<FarmWorkerDto>> Handle(
        GetFarmWorkersQuery request,
        CancellationToken cancellationToken)
    {
        // Verify farm exists first
        _ = await _uow.FishFarms.GetByIdAsync(request.FishFarmId, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(
                nameof(Domain.Entities.FishFarm), request.FishFarmId);

        var (items, total) = await _uow.FarmWorkers.GetPagedByFarmAsync(
            request.FishFarmId,
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.Position,
            request.CertExpired,
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dtos = items.Select(fw => new FarmWorkerDto
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
        }).ToList();

        return PaginatedResult<FarmWorkerDto>.Create(
            dtos, total, request.PageNumber, request.PageSize);
    }
}
