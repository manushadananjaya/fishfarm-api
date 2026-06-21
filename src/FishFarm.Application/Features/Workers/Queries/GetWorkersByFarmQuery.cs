using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Queries;

public sealed record GetWorkersByFarmQuery(Guid FishFarmId) : IRequest<IReadOnlyList<WorkerDto>>;

public sealed class GetWorkersByFarmQueryHandler
    : IRequestHandler<GetWorkersByFarmQuery, IReadOnlyList<WorkerDto>>
{
    private readonly IUnitOfWork _uow;

    public GetWorkersByFarmQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<WorkerDto>> Handle(
        GetWorkersByFarmQuery request,
        CancellationToken cancellationToken)
    {
        // Verify farm exists and is not soft-deleted
        var farm = await _uow.FishFarms.GetByIdAsync(request.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.FishFarm), request.FishFarmId);

        var workers = await _uow.Workers.GetByFishFarmAsync(request.FishFarmId, cancellationToken);

        return workers.Select(w => new WorkerDto
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
        }).ToList();
    }
}
