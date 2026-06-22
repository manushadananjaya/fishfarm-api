using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Queries;

public sealed record GetFishFarmsQuery(
    int     PageNumber = 1,
    int     PageSize   = 10,
    string? Search     = null,
    bool?   HasBarge   = null,
    int?    MinCages   = null,
    int?    MaxCages   = null)
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
            request.Search,
            request.HasBarge,
            request.MinCages,
            request.MaxCages,
            cancellationToken);

        var dtos = items.Select(p => new FishFarmSummaryDto
        {
            Id            = p.Farm.Id,
            Name          = p.Farm.Name,
            GpsLatitude   = p.Farm.GpsLatitude,
            GpsLongitude  = p.Farm.GpsLongitude,
            NumberOfCages = p.Farm.NumberOfCages,
            HasBarge      = p.Farm.HasBarge,
            PictureUrl    = p.Farm.PictureUrl,
            WorkerCount   = p.WorkerCount
        }).ToList();

        return PaginatedResult<FishFarmSummaryDto>.Create(dtos, total, request.PageNumber, request.PageSize);
    }
}
