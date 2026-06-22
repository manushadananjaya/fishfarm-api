using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Queries;

public sealed record GetWorkersByFarmQuery(
    Guid            FishFarmId,
    int             PageNumber   = 1,
    int             PageSize     = 20,
    string?         Search       = null,
    WorkerPosition? Position     = null,
    bool?           CertExpired  = null)
    : IRequest<PaginatedResult<WorkerDto>>;

public sealed class GetWorkersByFarmQueryHandler
    : IRequestHandler<GetWorkersByFarmQuery, PaginatedResult<WorkerDto>>
{
    private readonly IUnitOfWork _uow;

    public GetWorkersByFarmQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedResult<WorkerDto>> Handle(
        GetWorkersByFarmQuery request,
        CancellationToken cancellationToken)
    {
        // Verify farm exists and is not soft-deleted
        _ = await _uow.FishFarms.GetByIdAsync(request.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), request.FishFarmId);

        var (workers, total) = await _uow.Workers.GetPagedByFishFarmAsync(
            request.FishFarmId,
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.Position,
            request.CertExpired,
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dtos = workers.Select(w => new WorkerDto
        {
            Id             = w.Id,
            FishFarmId     = w.FishFarmId,
            Name           = w.Name,
            Age            = w.Age,
            Email          = w.Email,
            Position       = w.Position.ToString(),
            CertifiedUntil = w.CertifiedUntil,
            IsExpired      = w.CertifiedUntil < today,
            PictureUrl     = w.PictureUrl,
            CreatedAt      = w.CreatedAt,
            UpdatedAt      = w.UpdatedAt
        }).ToList();

        return PaginatedResult<WorkerDto>.Create(dtos, total, request.PageNumber, request.PageSize);
    }
}
