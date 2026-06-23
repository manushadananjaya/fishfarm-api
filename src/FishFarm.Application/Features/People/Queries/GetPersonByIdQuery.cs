using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Domain.Interfaces;
using MediatR;

namespace FishFarm.Application.Features.People.Queries;

public sealed record GetPersonByIdQuery(Guid Id) : IRequest<PersonDto>;

public sealed class GetPersonByIdQueryHandler : IRequestHandler<GetPersonByIdQuery, PersonDto>
{
    private readonly IUnitOfWork _uow;
    public GetPersonByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PersonDto> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        var person = await _uow.People.GetByIdWithAssignmentsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Person), request.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new PersonDto
        {
            Id             = person.Id,
            PersonCode     = $"P-{person.PersonNumber:D5}",
            Name           = person.Name,
            Email          = person.Email,
            Age            = person.Age,
            CertifiedUntil = person.CertifiedUntil,
            IsExpired      = person.CertifiedUntil < today,
            PictureUrl     = person.PictureUrl,
            CreatedAt      = person.CreatedAt,
            UpdatedAt      = person.UpdatedAt,
            Assignments    = person.FarmWorkers
                .Select(fw => new PersonFarmAssignmentDto
                {
                    FarmWorkerId = fw.Id,
                    FishFarmId   = fw.FishFarmId,
                    FarmName     = fw.FishFarm.Name,
                    FarmCode     = $"FF-{fw.FishFarm.FarmNumber:D5}",
                    Position     = fw.Position.ToString()
                })
                .ToList()
        };
    }
}
