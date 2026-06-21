using FishFarm.API.Controllers;
using FishFarm.Application.Features.FishFarms.Commands;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.FishFarms.Queries;
using FishFarm.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FishFarm.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="FishFarmsController"/>.
/// The controller is isolated: MediatR is mocked so only the routing and
/// response-shaping logic is under test.
/// </summary>
public sealed class FishFarmsControllerTests
{
    private readonly Mock<IMediator>      _mediatorMock;
    private readonly FishFarmsController  _sut;          // System Under Test

    public FishFarmsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut          = new FishFarmsController(_mediatorMock.Object);
    }

    // ── GET /api/fishfarms ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_DefaultPaging_Returns200WithPaginatedResult()
    {
        // Arrange
        var expected = TestDataFactory.CreatePaginatedResult();
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFishFarmsQuery>(q => q.PageNumber == 1 && q.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _sut.GetAll(1, 10, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(expected);

        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetFishFarmsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_PageSizeOver50_CapsAt50()
    {
        // Arrange
        var expected = TestDataFactory.CreatePaginatedResult();
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFishFarmsQuery>(q => q.PageSize == 50),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _sut.GetAll(1, 999, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();

        // Verify the query was sent with page size capped to 50
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetFishFarmsQuery>(q => q.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_CustomPaging_ForwardsPageNumberAndSize()
    {
        // Arrange
        var expected = TestDataFactory.CreatePaginatedResult(pageNumber: 3, pageSize: 5);
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFishFarmsQuery>(q => q.PageNumber == 3 && q.PageSize == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetAll(3, 5, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── GET /api/fishfarms/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingFarm_Returns200WithDto()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var expected = TestDataFactory.CreateFishFarmDto(farmId);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFishFarmByIdQuery>(q => q.Id == farmId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _sut.GetById(farmId, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_MediatorCalled_WithCorrectId()
    {
        // Arrange
        var farmId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFishFarmByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmDto(farmId));

        // Act
        await _sut.GetById(farmId, CancellationToken.None);

        // Assert – query must carry the exact requested ID
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetFishFarmByIdQuery>(q => q.Id == farmId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── POST /api/fishfarms ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201CreatedAtAction()
    {
        // Arrange
        var newId   = TestDataFactory.FarmId1;
        var request = TestDataFactory.CreateFishFarmRequest("Sunrise Farm");

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateFishFarmCommand>(c => c.Request.Name == "Sunrise Farm"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var actionResult = await _sut.Create(request, CancellationToken.None);

        // Assert
        var created = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(nameof(FishFarmsController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(newId);
    }

    [Fact]
    public async Task Create_WithPicture_IncludesFileInCommand()
    {
        // Arrange
        var mockFile = TestDataFactory.CreateMockFormFile();
        var request  = new CreateFishFarmRequest
        {
            Name          = "Farm With Picture",
            GpsLatitude   = 60.0m,
            GpsLongitude  = 5.0m,
            NumberOfCages = 4,
            HasBarge      = false,
            Picture       = mockFile
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateFishFarmCommand>(c => c.Request.Picture != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateFishFarmCommand>(c => c.Request.Picture != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── PUT /api/fishfarms/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidRequest_Returns204NoContent()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var request = TestDataFactory.UpdateFishFarmRequest("Renamed Farm");

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateFishFarmCommand>(c => c.Id == farmId && c.Request.Name == "Renamed Farm"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        var actionResult = await _sut.Update(farmId, request, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndRequest()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var request = TestDataFactory.UpdateFishFarmRequest();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateFishFarmCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await _sut.Update(farmId, request, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<UpdateFishFarmCommand>(c =>
                c.Id      == farmId &&
                c.Request == request),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── PATCH /api/fishfarms/{id}/picture ─────────────────────────────────────

    [Fact]
    public async Task UpdatePicture_ValidFile_Returns200WithPictureUrl()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var newUrl  = "https://res.cloudinary.com/test/fishfarms/new-pic.jpg";
        var request = new UpdateFishFarmPictureRequest
        {
            Picture = TestDataFactory.CreateMockFormFile()
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateFishFarmPictureCommand>(c => c.Id == farmId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUrl);

        // Act
        var actionResult = await _sut.UpdatePicture(farmId, request, CancellationToken.None);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        // The response body should contain a pictureUrl property
        var body = ok.Value!;
        var pictureUrlProp = body.GetType().GetProperty("pictureUrl");
        pictureUrlProp.Should().NotBeNull();
        pictureUrlProp!.GetValue(body).Should().Be(newUrl);
    }

    // ── DELETE /api/fishfarms/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingFarm_Returns204NoContent()
    {
        // Arrange
        var farmId = TestDataFactory.FarmId1;

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<DeleteFishFarmCommand>(c => c.Id == farmId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        var actionResult = await _sut.Delete(farmId, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectId()
    {
        // Arrange
        var farmId = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteFishFarmCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await _sut.Delete(farmId, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<DeleteFishFarmCommand>(c => c.Id == farmId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
