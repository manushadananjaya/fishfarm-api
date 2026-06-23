using FishFarm.API.Controllers;
using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.People.Commands;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Application.Features.People.Queries;
using FishFarm.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FishFarm.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="PeopleController"/>.
/// MediatR is mocked — only controller routing and response-shaping is under test.
/// </summary>
public sealed class PeopleControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly PeopleController _sut;

    public PeopleControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut          = new PeopleController(_mediatorMock.Object);
    }

    // ── GET /api/people ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_DefaultPaging_Returns200WithPaginatedResult()
    {
        // Arrange
        var summary = new PersonSummaryDto
        {
            Id             = TestDataFactory.PersonId1,
            PersonCode     = "P-00001",
            Name           = "John Fisher",
            Email          = "john.fisher@example.com",
            Age            = 30,
            CertifiedUntil = new DateOnly(2027, 12, 31),
            IsExpired      = false,
            FarmCount      = 2,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };
        var pagedResult = PaginatedResult<PersonSummaryDto>.Create(
            new[] { summary }, 1, 1, 20);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPeopleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var actionResult = await _sut.GetAll(cancellationToken: CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetAll_PageSizeOver100_CapsAt100()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPeopleQuery>(q => q.PageSize == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaginatedResult<PersonSummaryDto>.Create([], 0, 1, 100));

        // Act
        await _sut.GetAll(pageSize: 999, cancellationToken: CancellationToken.None);

        // Assert — mediator receives capped value
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetPeopleQuery>(q => q.PageSize == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GET /api/people/{personId} ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPerson_Returns200WithDto()
    {
        // Arrange
        var dto = new PersonDto
        {
            Id             = TestDataFactory.PersonId1,
            PersonCode     = "P-00001",
            Name           = "John Fisher",
            Email          = "john.fisher@example.com",
            Age            = 30,
            CertifiedUntil = new DateOnly(2027, 12, 31),
            IsExpired      = false,
            Assignments    = [],
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPersonByIdQuery>(q => q.Id == TestDataFactory.PersonId1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var actionResult = await _sut.GetById(TestDataFactory.PersonId1, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_UnknownPerson_PropagatesNotFoundException()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPersonByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Person", Guid.NewGuid()));

        var act = () => _sut.GetById(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── POST /api/people ───────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithId()
    {
        // Arrange
        var newId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreatePersonCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        var request = new CreatePersonRequest
        {
            Name           = "Alice Diver",
            Email          = "alice@example.com",
            Age            = 28,
            CertifiedUntil = new DateOnly(2027, 6, 1)
        };

        // Act
        var actionResult = await _sut.Create(request, CancellationToken.None);

        // Assert
        var created = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(nameof(PeopleController.GetById));
    }

    // ── PUT /api/people/{personId} ─────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingPerson_Returns204()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdatePersonCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MediatR.Unit.Value));

        var request = new UpdatePersonRequest
        {
            Name           = "Updated",
            Email          = "u@u.com",
            Age            = 32,
            CertifiedUntil = new DateOnly(2028, 1, 1)
        };

        // Act
        var actionResult = await _sut.Update(TestDataFactory.PersonId1, request, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    // ── DELETE /api/people/{personId} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingPerson_Returns204()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeletePersonCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MediatR.Unit.Value));

        var actionResult = await _sut.Delete(TestDataFactory.PersonId1, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }
}
