using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace FishFarm.Application.Features.FishFarms.Queries;

// ─────────────────────────────────────────────────────────────────────────────
//  Query record
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns full farm details (GPS, properties, active workers) for all active farms.
/// Optionally constrained to a geographic bounding box.
/// The response is designed so the frontend can render both a map marker AND a
/// farm detail panel without a second round-trip.
/// </summary>
public sealed record GetFishFarmsMapQuery(
    decimal? North = null,
    decimal? South = null,
    decimal? East  = null,
    decimal? West  = null)
    : IRequest<IReadOnlyList<FishFarmMapDto>>;

// ─────────────────────────────────────────────────────────────────────────────
//  Handler
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GetFishFarmsMapQueryHandler
    : IRequestHandler<GetFishFarmsMapQuery, IReadOnlyList<FishFarmMapDto>>
{
    private readonly IUnitOfWork _uow;

    public GetFishFarmsMapQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<FishFarmMapDto>> Handle(
        GetFishFarmsMapQuery request,
        CancellationToken cancellationToken)
    {
        var farms = await _uow.FishFarms.GetMapAsync(
            request.North,
            request.South,
            request.East,
            request.West,
            cancellationToken);

        return farms.Select(farm => new FishFarmMapDto
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
            // Global query filter on FarmWorker means only active assignments are counted.
            WorkerCount   = farm.FarmWorkers.Count
        }).ToList();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Validator — runs automatically via ValidationBehaviour pipeline
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GetFishFarmsMapQueryValidator : AbstractValidator<GetFishFarmsMapQuery>
{
    // WGS-84 valid ranges
    private const decimal MinLat  = -90m;
    private const decimal MaxLat  =  90m;
    private const decimal MinLon  = -180m;
    private const decimal MaxLon  =  180m;

    public GetFishFarmsMapQueryValidator()
    {
        When(x => x.North.HasValue, () =>
            RuleFor(x => x.North!.Value)
                .InclusiveBetween(MinLat, MaxLat)
                .WithMessage($"North must be a valid latitude between {MinLat} and {MaxLat}."));

        When(x => x.South.HasValue, () =>
            RuleFor(x => x.South!.Value)
                .InclusiveBetween(MinLat, MaxLat)
                .WithMessage($"South must be a valid latitude between {MinLat} and {MaxLat}."));

        When(x => x.East.HasValue, () =>
            RuleFor(x => x.East!.Value)
                .InclusiveBetween(MinLon, MaxLon)
                .WithMessage($"East must be a valid longitude between {MinLon} and {MaxLon}."));

        When(x => x.West.HasValue, () =>
            RuleFor(x => x.West!.Value)
                .InclusiveBetween(MinLon, MaxLon)
                .WithMessage($"West must be a valid longitude between {MinLon} and {MaxLon}."));

        // Bounding box sanity: north must be above south, east must be right of west.
        When(x => x.North.HasValue && x.South.HasValue, () =>
            RuleFor(x => x.North!.Value)
                .GreaterThan(x => x.South!.Value)
                .WithMessage("North must be greater than South."));

        When(x => x.East.HasValue && x.West.HasValue, () =>
            RuleFor(x => x.East!.Value)
                .GreaterThan(x => x.West!.Value)
                .WithMessage("East must be greater than West (anti-meridian spanning is not supported)."));
    }
}
