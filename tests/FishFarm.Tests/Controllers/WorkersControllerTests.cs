using FishFarm.API.Controllers;
using FishFarm.Application.Features.Workers.Commands;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Application.Features.Workers.Queries;
using FishFarm.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FishFarm.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="WorkersController"/>.
/// MediatR is mocked so only controller routing and response-shaping is exercised.
/// </summary>
public sealed class WorkersControllerTests
{
    private readonly Mock<IMediator>    _mediatorMock;
    private readonly WorkersController  _sut;

    public WorkersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut          = new WorkersController(_mediatorMock.Object);
    }

    // ── GET /api/fishfarms/{fishFarmId}/workers ────────────────────────────────

    [Fact]
    public async Task GetAll_ExistingFarm_Returns200WithWorkerList()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workers  = new List<WorkerDto>
        {
            TestDataFactory.CreateWorkerDto(TestDataFactory.WorkerId1, farmId),
            TestDataFactory.CreateWorkerDto(TestDataFactory.WorkerId2, farmId)
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetWorkersByFarmQuery>(q => q.FishFarmId == farmId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(workers);

        // Act
        var actionResult = await _sut.GetAll(farmId, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(workers);
    }

    [Fact]
    public async Task GetAll_SendsQueryWithCorrectFarmId()
    {
        // Arrange
        var farmId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetWorkersByFarmQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkerDto>());

        // Act
        await _sut.GetAll(farmId, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetWorkersByFarmQuery>(q => q.FishFarmId == farmId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_EmptyFarm_Returns200WithEmptyList()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetWorkersByFarmQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkerDto>());

        // Act
        var result = await _sut.GetAll(TestDataFactory.FarmId1, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.As<List<WorkerDto>>().Should().BeEmpty();
    }

    // ── GET /api/fishfarms/{fishFarmId}/workers/{workerId} ────────────────────

    [Fact]
    public async Task GetById_ExistingWorker_Returns200WithDto()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        var expected = TestDataFactory.CreateWorkerDto(workerId, farmId);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetWorkerByIdQuery>(q => q.FishFarmId == farmId && q.WorkerId == workerId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _sut.GetById(farmId, workerId, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_SendsQueryWithBothIds()
    {
        // Arrange
        var farmId   = Guid.NewGuid();
        var workerId = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetWorkerByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateWorkerDto(workerId, farmId));

        // Act
        await _sut.GetById(farmId, workerId, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetWorkerByIdQuery>(q =>
                q.FishFarmId == farmId &&
                q.WorkerId   == workerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── POST /api/fishfarms/{fishFarmId}/workers ───────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201CreatedAtAction()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var newId    = TestDataFactory.WorkerId1;
        var request  = TestDataFactory.CreateWorkerRequest("Alice Marine");

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateWorkerCommand>(c =>
                    c.FishFarmId        == farmId &&
                    c.Request.Name      == "Alice Marine"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var actionResult = await _sut.Create(farmId, request, CancellationToken.None);

        // Assert
        var created = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(nameof(WorkersController.GetById));
        created.RouteValues.Should().ContainKey("fishFarmId").WhoseValue.Should().Be(farmId);
        created.RouteValues.Should().ContainKey("workerId").WhoseValue.Should().Be(newId);
    }

    [Fact]
    public async Task Create_WithProfilePicture_IncludesFileInCommand()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var mockFile = TestDataFactory.CreateMockFormFile("worker.jpg");
        var request  = new CreateWorkerRequest
        {
            Name           = "Bob Nets",
            Age            = 27,
            Email          = "bob@example.com",
            Position       = Domain.Enums.WorkerPosition.Worker,
            CertifiedUntil = new DateOnly(2027, 1, 1),
            Picture        = mockFile
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateWorkerCommand>(c => c.Request.Picture != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.Create(farmId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    // ── PUT /api/fishfarms/{fishFarmId}/workers/{workerId} ────────────────────

    [Fact]
    public async Task Update_ValidRequest_Returns204NoContent()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        var request  = TestDataFactory.UpdateWorkerRequest("Updated Name");

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateWorkerCommand>(c =>
                    c.FishFarmId == farmId &&
                    c.WorkerId   == workerId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        var actionResult = await _sut.Update(farmId, workerId, request, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Update_SendsCommandWithAllThreeIds()
    {
        // Arrange
        var farmId   = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var request  = TestDataFactory.UpdateWorkerRequest();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateWorkerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await _sut.Update(farmId, workerId, request, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<UpdateWorkerCommand>(c =>
                c.FishFarmId == farmId  &&
                c.WorkerId   == workerId &&
                c.Request    == request),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── PATCH /api/fishfarms/{fishFarmId}/workers/{workerId}/picture ───────────

    [Fact]
    public async Task UpdatePicture_ValidFile_Returns200WithPictureUrl()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        var newUrl   = "https://res.cloudinary.com/test/workers/new-pic.jpg";
        var request  = new UpdateWorkerPictureRequest
        {
            Picture = TestDataFactory.CreateMockFormFile()
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateWorkerPictureCommand>(c =>
                    c.FishFarmId == farmId && c.WorkerId == workerId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUrl);

        // Act
        var actionResult = await _sut.UpdatePicture(farmId, workerId, request, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var body           = ok.Value!;
        var pictureUrlProp = body.GetType().GetProperty("pictureUrl");
        pictureUrlProp.Should().NotBeNull();
        pictureUrlProp!.GetValue(body).Should().Be(newUrl);
    }

    // ── DELETE /api/fishfarms/{fishFarmId}/workers/{workerId} ──────────────────

    [Fact]
    public async Task Delete_ExistingWorker_Returns204NoContent()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<DeleteWorkerCommand>(c =>
                    c.FishFarmId == farmId && c.WorkerId == workerId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        var actionResult = await _sut.Delete(farmId, workerId, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectBothIds()
    {
        // Arrange
        var farmId   = Guid.NewGuid();
        var workerId = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteWorkerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await _sut.Delete(farmId, workerId, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<DeleteWorkerCommand>(c =>
                c.FishFarmId == farmId  &&
                c.WorkerId   == workerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
