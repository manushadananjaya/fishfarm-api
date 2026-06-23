using FishFarm.API.Controllers;
using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FarmWorkers.Commands;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Application.Features.FarmWorkers.Queries;
using FishFarm.Domain.Enums;
using FishFarm.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FishFarm.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="WorkersController"/>.
/// MediatR is mocked — only routing and response-shaping is under test.
/// </summary>
public sealed class WorkersControllerTests
{
    private readonly Mock<IMediator>  _mediatorMock;
    private readonly WorkersController _sut;

    public WorkersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut          = new WorkersController(_mediatorMock.Object);
    }

    // ── GET /api/fishfarms/{farmId}/workers ───────────────────────────────────

    [Fact]
    public async Task GetAll_DefaultPaging_Returns200WithPaginatedResult()
    {
        // Arrange
        var dto    = TestDataFactory.CreateFarmWorkerDto();
        var paged  = PaginatedResult<FarmWorkerDto>.Create(new[] { dto }, 1, 1, 20);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFarmWorkersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var actionResult = await _sut.GetAll(
            TestDataFactory.FarmId1, cancellationToken: CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(paged);
    }

    [Fact]
    public async Task GetAll_PageSizeOver100_CapsAt100()
    {
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFarmWorkersQuery>(q => q.PageSize == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaginatedResult<FarmWorkerDto>.Create([], 0, 1, 100));

        await _sut.GetAll(TestDataFactory.FarmId1, pageSize: 999, cancellationToken: CancellationToken.None);

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetFarmWorkersQuery>(q => q.PageSize == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GET /api/fishfarms/{farmId}/workers/{farmWorkerId} ────────────────────

    [Fact]
    public async Task GetById_ExistingAssignment_Returns200WithDto()
    {
        // Arrange
        var dto = TestDataFactory.CreateFarmWorkerDto();
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFarmWorkerByIdQuery>(q =>
                    q.FishFarmId  == TestDataFactory.FarmId1 &&
                    q.FarmWorkerId == TestDataFactory.FwId1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var actionResult = await _sut.GetById(
            TestDataFactory.FarmId1, TestDataFactory.FwId1, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_UnknownAssignment_PropagatesNotFoundException()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFarmWorkerByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("FarmWorker", Guid.NewGuid()));

        var act = () => _sut.GetById(
            TestDataFactory.FarmId1, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── POST /api/fishfarms/{farmId}/workers ──────────────────────────────────

    [Fact]
    public async Task Assign_ValidRequest_Returns201WithId()
    {
        // Arrange
        var newId = TestDataFactory.FwId1;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AssignPersonToFarmCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        var request = new AssignPersonToFarmRequest
        {
            PersonId = TestDataFactory.PersonId1,
            Position = WorkerPosition.Worker
        };

        // Act
        var actionResult = await _sut.Assign(
            TestDataFactory.FarmId1, request, CancellationToken.None);

        // Assert
        var created = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(nameof(WorkersController.GetById));
    }

    // ── PUT /api/fishfarms/{farmId}/workers/{farmWorkerId} ────────────────────

    [Fact]
    public async Task UpdatePosition_ExistingAssignment_Returns204()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateFarmWorkerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MediatR.Unit.Value));

        var request = new UpdateFarmWorkerRequest { Position = WorkerPosition.CEO };

        var actionResult = await _sut.UpdatePosition(
            TestDataFactory.FarmId1, TestDataFactory.FwId1, request, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    // ── DELETE /api/fishfarms/{farmId}/workers/{farmWorkerId} ─────────────────

    [Fact]
    public async Task Remove_ExistingAssignment_Returns204()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveFarmWorkerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MediatR.Unit.Value));

        var actionResult = await _sut.Remove(
            TestDataFactory.FarmId1, TestDataFactory.FwId1, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Remove_UnknownAssignment_PropagatesNotFoundException()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveFarmWorkerCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("FarmWorker", Guid.NewGuid()));

        var act = () => _sut.Remove(
            TestDataFactory.FarmId1, TestDataFactory.FwId1, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
