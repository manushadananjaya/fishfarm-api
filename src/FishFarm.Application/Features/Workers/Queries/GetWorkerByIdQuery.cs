using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.Workers.Queries;

public sealed record GetWorkerByIdQuery(Guid FishFarmId, Guid WorkerId) : IRequest<WorkerDto>;

public sealed class GetWorkerByIdQueryHandler
    : IRequestHandler<GetWorkerByIdQuery, WorkerDto>
{
    private readonly IUnitOfWork _uow;

    public GetWorkerByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<WorkerDto> Handle(
        GetWorkerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var worker = await _uow.Workers.GetByIdAndFarmAsync(
            request.WorkerId, request.FishFarmId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worker), request.WorkerId);

        return new WorkerDto
        {
            Id             = worker.Id,
            FishFarmId     = worker.FishFarmId,
            Name           = worker.Name,
            Age            = worker.Age,
            Email          = worker.Email,
            Position       = worker.Position.ToString(),
            CertifiedUntil = worker.CertifiedUntil,
            PictureUrl     = worker.PictureUrl,
            CreatedAt      = worker.CreatedAt,
            UpdatedAt      = worker.UpdatedAt
        };
    }
}
