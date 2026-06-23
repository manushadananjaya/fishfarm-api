using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FishFarms.Commands;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.FishFarms.Queries;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using FishFarm.Tests.Common;
using FluentAssertions;
using Moq;

namespace FishFarm.Tests.Features.FishFarms;

/// <summary>
/// Unit tests for the FishFarm CQRS handlers.
/// The Application layer is fully isolated: <see cref="IUnitOfWork"/> and
/// <see cref="Application.Common.Interfaces.ICloudinaryService"/> are mocked.
/// </summary>
public sealed class FishFarmHandlerTests
{
    private readonly Mock<IUnitOfWork>                                        _uowMock;
    private readonly Mock<IFishFarmRepository>                                _farmRepoMock;
    private readonly Mock<IFarmWorkerRepository>                              _fwRepoMock;
    private readonly Mock<Application.Common.Interfaces.ICloudinaryService>   _cloudinaryMock;

    public FishFarmHandlerTests()
    {
        _uowMock        = new Mock<IUnitOfWork>();
        _farmRepoMock   = new Mock<IFishFarmRepository>();
        _fwRepoMock     = new Mock<IFarmWorkerRepository>();
        _cloudinaryMock = new Mock<Application.Common.Interfaces.ICloudinaryService>();

        _uowMock.Setup(u => u.FishFarms).Returns(_farmRepoMock.Object);
        _uowMock.Setup(u => u.FarmWorkers).Returns(_fwRepoMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetFishFarmsQueryHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFishFarmsHandler_ReturnsPagedResult()
    {
        // Arrange
        var farm1 = TestDataFactory.CreateFishFarmEntity(TestDataFactory.FarmId1, "Farm A");
        var farm2 = TestDataFactory.CreateFishFarmEntity(TestDataFactory.FarmId2, "Farm B");

        _farmRepoMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)>)
                    new List<(Domain.Entities.FishFarm, int)> { (farm1, 0), (farm2, 0) },
                2));

        var handler = new GetFishFarmsQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetFishFarmsQuery(1, 10), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].Name.Should().Be("Farm A");
        result.Items[1].Name.Should().Be("Farm B");
    }

    [Fact]
    public async Task GetFishFarmsHandler_MapsWorkerCountCorrectly()
    {
        // Arrange — WorkerCount comes from the repository projection, not from
        // farm.FarmWorkers in memory. The mock returns (farm, 3) directly.
        var farm = TestDataFactory.CreateFishFarmEntity(TestDataFactory.FarmId1, "Farm With Workers");

        _farmRepoMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)>)
                    new List<(Domain.Entities.FishFarm, int)> { (farm, 3) },
                1));

        var handler = new GetFishFarmsQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetFishFarmsQuery(1, 10), CancellationToken.None);

        // Assert
        result.Items[0].WorkerCount.Should().Be(3);
    }

    [Fact]
    public async Task GetFishFarmsHandler_EmptyPage_ReturnsEmptyList()
    {
        _farmRepoMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)>)
                    new List<(Domain.Entities.FishFarm, int)>(),
                0));

        var handler = new GetFishFarmsQueryHandler(_uowMock.Object);
        var result  = await handler.Handle(new GetFishFarmsQuery(99, 10), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetFishFarmByIdQueryHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFishFarmByIdHandler_ExistingFarm_ReturnsMappedDto()
    {
        var farmId = TestDataFactory.FarmId1;
        var farm   = TestDataFactory.CreateFishFarmEntity(farmId, "Atlantic Salmon Farm");

        _farmRepoMock
            .Setup(r => r.GetWithFarmWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);

        var handler = new GetFishFarmByIdQueryHandler(_uowMock.Object);
        var result  = await handler.Handle(new GetFishFarmByIdQuery(farmId), CancellationToken.None);

        result.Id.Should().Be(farmId);
        result.Name.Should().Be("Atlantic Salmon Farm");
        result.NumberOfCages.Should().Be(12);
        result.HasBarge.Should().BeTrue();
    }

    [Fact]
    public async Task GetFishFarmByIdHandler_FarmNotFound_ThrowsNotFoundException()
    {
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetWithFarmWorkersAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new GetFishFarmByIdQueryHandler(_uowMock.Object);
        var act     = async () => await handler.Handle(
            new GetFishFarmByIdQuery(missing), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*FishFarm*{missing}*");
    }

    [Fact]
    public async Task GetFishFarmByIdHandler_ProjectsWorkerIsExpiredCorrectly()
    {
        // Arrange — EF global query filter ensures GetWithFarmWorkersAsync never
        // returns soft-deleted assignments. Mock models that contract.
        var farmId  = TestDataFactory.FarmId1;
        var farm    = TestDataFactory.CreateFishFarmEntity(farmId);

        var expiredPerson = TestDataFactory.CreatePersonEntity(
            TestDataFactory.PersonId1,
            certifiedUntil: new DateOnly(2020, 1, 1));   // past date → expired

        var activePerson  = TestDataFactory.CreatePersonEntity(
            TestDataFactory.PersonId2,
            email:         "active@example.com",
            certifiedUntil: new DateOnly(2099, 12, 31)); // far future → active

        var fw1 = TestDataFactory.CreateFarmWorkerEntity(
            TestDataFactory.FwId1, farmId, TestDataFactory.PersonId1, person: expiredPerson);
        var fw2 = TestDataFactory.CreateFarmWorkerEntity(
            TestDataFactory.FwId2, farmId, TestDataFactory.PersonId2, person: activePerson);

        farm.AddFarmWorker(fw1);
        farm.AddFarmWorker(fw2);

        _farmRepoMock
            .Setup(r => r.GetWithFarmWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);

        var handler = new GetFishFarmByIdQueryHandler(_uowMock.Object);
        var result  = await handler.Handle(new GetFishFarmByIdQuery(farmId), CancellationToken.None);

        // Assert — both assignments returned with correctly computed IsExpired
        result.Workers.Should().HaveCount(2);
        result.Workers.Single(w => w.PersonId == TestDataFactory.PersonId1).IsExpired.Should().BeTrue();
        result.Workers.Single(w => w.PersonId == TestDataFactory.PersonId2).IsExpired.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CreateFishFarmCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFishFarmHandler_NoPicture_CreatesAndReturnsId()
    {
        _farmRepoMock
            .Setup(r => r.AddAsync(
                It.IsAny<Domain.Entities.FishFarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = TestDataFactory.CreateFishFarmRequest("New Farm");
        var handler = new CreateFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var newId   = await handler.Handle(new CreateFishFarmCommand(request), CancellationToken.None);

        newId.Should().NotBeEmpty();
        _farmRepoMock.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.FishFarm>(f => f.Name == "New Farm"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.UploadImageAsync(
            It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFishFarmHandler_WithPicture_UploadsAndStoresUrl()
    {
        const string cloudinaryUrl = "https://res.cloudinary.com/demo/fishfarms/farm.jpg";
        const string publicId      = "fishfarms/farm";

        var mockFile = TestDataFactory.CreateMockFormFile();
        var request  = new CreateFishFarmRequest
        {
            Name          = "Scenic Farm",
            GpsLatitude   = 60.0m,
            GpsLongitude  = 5.0m,
            NumberOfCages = 6,
            HasBarge      = true,
            Picture       = mockFile
        };

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(mockFile, "fishfarms", It.IsAny<CancellationToken>()))
            .ReturnsAsync((cloudinaryUrl, publicId));
        _farmRepoMock
            .Setup(r => r.AddAsync(
                It.IsAny<Domain.Entities.FishFarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var newId   = await handler.Handle(new CreateFishFarmCommand(request), CancellationToken.None);

        newId.Should().NotBeEmpty();
        _farmRepoMock.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.FishFarm>(f =>
                f.Name           == "Scenic Farm" &&
                f.PictureUrl     == cloudinaryUrl  &&
                f.PicturePublicId == publicId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdateFishFarmCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateFishFarmHandler_ExistingFarm_UpdatesPropertiesAndSaves()
    {
        var farmId  = TestDataFactory.FarmId1;
        var farm    = TestDataFactory.CreateFishFarmEntity(farmId, "Old Name");
        var request = TestDataFactory.UpdateFishFarmRequest("New Name");

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _farmRepoMock.Setup(r => r.Update(It.IsAny<Domain.Entities.FishFarm>()));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateFishFarmCommandHandler(_uowMock.Object);
        await handler.Handle(new UpdateFishFarmCommand(farmId, request), CancellationToken.None);

        farm.Name.Should().Be("New Name");
        farm.HasBarge.Should().Be(request.HasBarge);
        _farmRepoMock.Verify(r => r.Update(farm), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFishFarmHandler_FarmNotFound_ThrowsNotFoundException()
    {
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new UpdateFishFarmCommandHandler(_uowMock.Object);
        var act     = async () => await handler.Handle(
            new UpdateFishFarmCommand(missing, TestDataFactory.UpdateFishFarmRequest()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdateFishFarmPictureCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateFishFarmPictureHandler_DeletesOldAndUploadsNew()
    {
        var farmId        = TestDataFactory.FarmId1;
        const string oldPublicId = "fishfarms/old-pic";
        const string newUrl      = "https://res.cloudinary.com/demo/fishfarms/new.jpg";
        const string newPublicId = "fishfarms/new-pic";

        var farm     = TestDataFactory.CreateFishFarmEntity(farmId, picturePublicId: oldPublicId);
        var mockFile = TestDataFactory.CreateMockFormFile();
        var request  = new UpdateFishFarmPictureRequest { Picture = mockFile };

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(oldPublicId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(mockFile, "fishfarms", It.IsAny<CancellationToken>()))
            .ReturnsAsync((newUrl, newPublicId));
        _farmRepoMock.Setup(r => r.Update(It.IsAny<Domain.Entities.FishFarm>()));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateFishFarmPictureCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var result  = await handler.Handle(
            new UpdateFishFarmPictureCommand(farmId, request), CancellationToken.None);

        result.Should().Be(newUrl);
        farm.PictureUrl.Should().Be(newUrl);
        farm.PicturePublicId.Should().Be(newPublicId);
        _cloudinaryMock.Verify(
            c => c.DeleteImageAsync(oldPublicId, It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(
            c => c.UploadImageAsync(mockFile, "fishfarms", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DeleteFishFarmCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteFishFarmHandler_ExistingFarm_SoftDeletesFarmAndAssignments()
    {
        // Arrange
        var farmId = TestDataFactory.FarmId1;
        var farm   = TestDataFactory.CreateFishFarmEntity(farmId, picturePublicId: "fishfarms/farm-pic");

        var assignment = TestDataFactory.CreateFarmWorkerEntity(fishFarmId: farmId);
        farm.AddFarmWorker(assignment);

        _farmRepoMock
            .Setup(r => r.GetWithFarmWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeleteFishFarmCommand(farmId), CancellationToken.None);

        // Assert — assignment and farm soft-deleted, farm picture cleaned up
        _fwRepoMock.Verify(r => r.Delete(assignment), Times.Once);
        _farmRepoMock.Verify(r => r.Delete(farm), Times.Once);
        _cloudinaryMock.Verify(
            c => c.DeleteImageAsync("fishfarms/farm-pic", It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFishFarmHandler_FarmNotFound_ThrowsNotFoundException()
    {
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetWithFarmWorkersAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new DeleteFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var act     = async () => await handler.Handle(
            new DeleteFishFarmCommand(missing), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteFishFarmHandler_FarmWithNoAssignments_OnlyFarmDeleted()
    {
        // EF global query filter ensures GetWithFarmWorkersAsync never includes
        // soft-deleted assignments — this test models a farm with zero active assignments.
        var farmId = TestDataFactory.FarmId1;
        var farm   = TestDataFactory.CreateFishFarmEntity(farmId); // no FarmWorkers added

        _farmRepoMock
            .Setup(r => r.GetWithFarmWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        await handler.Handle(new DeleteFishFarmCommand(farmId), CancellationToken.None);

        _fwRepoMock.Verify(r => r.Delete(It.IsAny<FarmWorker>()), Times.Never);
        _farmRepoMock.Verify(r => r.Delete(farm), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
