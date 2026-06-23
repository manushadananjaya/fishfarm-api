using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Queries;

public sealed record GetPeopleQuery(
    int     PageNumber  = 1,
    int     PageSize    = 20,
    string? Search      = null,
    bool?   CertExpired = null) : IRequest<PaginatedResult<PersonSummaryDto>>;

public sealed class GetPeopleQueryHandler
    : IRequestHandler<GetPeopleQuery, PaginatedResult<PersonSummaryDto>>
{
    private readonly IUnitOfWork _uow;
    public GetPeopleQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedResult<PersonSummaryDto>> Handle(
        GetPeopleQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _uow.People.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.CertExpired,
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dtos = items.Select(t => new PersonSummaryDto
        {
            Id             = t.Person.Id,
            PersonCode     = $"P-{t.Person.PersonNumber:D5}",
            Name           = t.Person.Name,
            Email          = t.Person.Email,
            Age            = t.Person.Age,
            CertifiedUntil = t.Person.CertifiedUntil,
            IsExpired      = t.Person.CertifiedUntil < today,
            PictureUrl     = t.Person.PictureUrl,
            FarmCount      = t.FarmCount,
            CreatedAt      = t.Person.CreatedAt,
            UpdatedAt      = t.Person.UpdatedAt
        }).ToList();

        return PaginatedResult<PersonSummaryDto>.Create(
            dtos, total, request.PageNumber, request.PageSize);
    }
}
