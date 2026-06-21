using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Queries;

public sealed record GetFishFarmsQuery(int PageNumber = 1, int PageSize = 10)
    : IRequest<PaginatedResult<FishFarmSummaryDto>>;

public sealed class GetFishFarmsQueryHandler
    : IRequestHandler<GetFishFarmsQuery, PaginatedResult<FishFarmSummaryDto>>
{
    private readonly IUnitOfWork _uow;

    public GetFishFarmsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedResult<FishFarmSummaryDto>> Handle(
        GetFishFarmsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _uow.FishFarms.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(f => new FishFarmSummaryDto
        {
            Id            = f.Id,
            Name          = f.Name,
            GpsLatitude   = f.GpsLatitude,
            GpsLongitude  = f.GpsLongitude,
            NumberOfCages = f.NumberOfCages,
            HasBarge      = f.HasBarge,
            PictureUrl    = f.PictureUrl,
            WorkerCount   = f.Workers.Count(w => !w.IsDeleted)
        }).ToList();

        return PaginatedResult<FishFarmSummaryDto>.Create(dtos, total, request.PageNumber, request.PageSize);
    }
}
