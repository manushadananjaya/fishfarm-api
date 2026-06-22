using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FishFarms.Commands;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.FishFarms.Queries;
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
    private readonly Mock<Application.Common.Interfaces.ICloudinaryService>   _cloudinaryMock;

    public FishFarmHandlerTests()
    {
        _uowMock        = new Mock<IUnitOfWork>();
        _farmRepoMock   = new Mock<IFishFarmRepository>();
        _cloudinaryMock = new Mock<Application.Common.Interfaces.ICloudinaryService>();

        // Wire the repo to the UoW
        _uowMock.Setup(u => u.FishFarms).Returns(_farmRepoMock.Object);
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
        var items = new List<Domain.Entities.FishFarm> { farm1, farm2 };

        _farmRepoMock
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)>)
                    new List<(Domain.Entities.FishFarm, int)> { (farm1, 0), (farm2, 0) },
                2));

        var handler = new GetFishFarmsQueryHandler(_uowMock.Object);
        var query   = new GetFishFarmsQuery(1, 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items[0].Name.Should().Be("Farm A");
        result.Items[1].Name.Should().Be("Farm B");
    }

    [Fact]
    public async Task GetFishFarmsHandler_MapsWorkerCountCorrectly()
    {
        // Arrange
        var farm = TestDataFactory.CreateFishFarmEntity(TestDataFactory.FarmId1, "Farm With Workers");
        var worker1 = TestDataFactory.CreateWorkerEntity();
        var worker2 = TestDataFactory.CreateWorkerEntity(Guid.NewGuid(), email: "w2@example.com");
        farm.AddWorker(worker1);
        farm.AddWorker(worker2);

        // The handler now reads WorkerCount directly from the repository projection.
        _farmRepoMock
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)>)
                    new List<(Domain.Entities.FishFarm, int)> { (farm, 2) },
                1));

        var handler = new GetFishFarmsQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetFishFarmsQuery(1, 10), CancellationToken.None);

        // Assert
        result.Items[0].WorkerCount.Should().Be(2);
    }

    [Fact]
    public async Task GetFishFarmsHandler_EmptyPage_ReturnsEmptyList()
    {
        // Arrange
        _farmRepoMock
            .Setup(r => r.GetPagedAsync(99, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Domain.Entities.FishFarm Farm, int WorkerCount)>)
                    new List<(Domain.Entities.FishFarm, int)>(),
                0));

        var handler = new GetFishFarmsQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetFishFarmsQuery(99, 10), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetFishFarmByIdQueryHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFishFarmByIdHandler_ExistingFarm_ReturnsMappedDto()
    {
        // Arrange
        var farmId = TestDataFactory.FarmId1;
        var farm   = TestDataFactory.CreateFishFarmEntity(farmId, "Atlantic Salmon Farm");

        _farmRepoMock
            .Setup(r => r.GetWithWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);

        var handler = new GetFishFarmByIdQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetFishFarmByIdQuery(farmId), CancellationToken.None);

        // Assert
        result.Id.Should().Be(farmId);
        result.Name.Should().Be("Atlantic Salmon Farm");
        result.NumberOfCages.Should().Be(12);
        result.HasBarge.Should().BeTrue();
    }

    [Fact]
    public async Task GetFishFarmByIdHandler_FarmNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetWithWorkersAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new GetFishFarmByIdQueryHandler(_uowMock.Object);

        // Act
        var act = async () => await handler.Handle(new GetFishFarmByIdQuery(missing), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*FishFarm*{missing}*");
    }

    [Fact]
    public async Task GetFishFarmByIdHandler_ProjectsWorkerIsExpiredCorrectly()
    {
        // Arrange
        // EF's global query filter on Worker excludes soft-deleted rows before they reach the
        // handler — the mock models that contract by only returning active workers.
        var farmId  = TestDataFactory.FarmId1;
        var farm    = TestDataFactory.CreateFishFarmEntity(farmId);
        var expired = TestDataFactory.CreateWorkerEntity(
            TestDataFactory.WorkerId1,
            certifiedUntil: new DateOnly(2020, 1, 1));   // cert in the past
        var active  = TestDataFactory.CreateWorkerEntity(
            TestDataFactory.WorkerId2,
            email: "active@example.com",
            certifiedUntil: new DateOnly(2099, 12, 31));  // cert in the future
        farm.AddWorker(expired);
        farm.AddWorker(active);

        _farmRepoMock
            .Setup(r => r.GetWithWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);

        var handler = new GetFishFarmByIdQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetFishFarmByIdQuery(farmId), CancellationToken.None);

        // Assert – both workers projected; IsExpired computed correctly for each
        result.Workers.Should().HaveCount(2);
        result.Workers.Single(w => w.Id == TestDataFactory.WorkerId1).IsExpired.Should().BeTrue();
        result.Workers.Single(w => w.Id == TestDataFactory.WorkerId2).IsExpired.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CreateFishFarmCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFishFarmHandler_NoPicture_CreatesAndReturnsId()
    {
        // Arrange
        _farmRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Domain.Entities.FishFarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = TestDataFactory.CreateFishFarmRequest("New Farm");
        var handler = new CreateFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var newId = await handler.Handle(new CreateFishFarmCommand(request), CancellationToken.None);

        // Assert
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
        // Arrange
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
            .Setup(r => r.AddAsync(It.IsAny<Domain.Entities.FishFarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var newId = await handler.Handle(new CreateFishFarmCommand(request), CancellationToken.None);

        // Assert
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
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var farm    = TestDataFactory.CreateFishFarmEntity(farmId, "Old Name");
        var request = TestDataFactory.UpdateFishFarmRequest("New Name");

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _farmRepoMock.Setup(r => r.Update(It.IsAny<Domain.Entities.FishFarm>()));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateFishFarmCommandHandler(_uowMock.Object);

        // Act
        await handler.Handle(new UpdateFishFarmCommand(farmId, request), CancellationToken.None);

        // Assert
        farm.Name.Should().Be("New Name");
        farm.HasBarge.Should().Be(request.HasBarge);
        _farmRepoMock.Verify(r => r.Update(farm), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFishFarmHandler_FarmNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new UpdateFishFarmCommandHandler(_uowMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new UpdateFishFarmCommand(missing, TestDataFactory.UpdateFishFarmRequest()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdateFishFarmPictureCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateFishFarmPictureHandler_DeletesOldAndUploadsNew()
    {
        // Arrange
        var farmId     = TestDataFactory.FarmId1;
        const string oldPublicId  = "fishfarms/old-pic";
        const string newUrl       = "https://res.cloudinary.com/demo/fishfarms/new.jpg";
        const string newPublicId  = "fishfarms/new-pic";

        var farm    = TestDataFactory.CreateFishFarmEntity(farmId, picturePublicId: oldPublicId);
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

        // Act
        var result = await handler.Handle(
            new UpdateFishFarmPictureCommand(farmId, request), CancellationToken.None);

        // Assert
        result.Should().Be(newUrl);
        farm.PictureUrl.Should().Be(newUrl);
        farm.PicturePublicId.Should().Be(newPublicId);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync(oldPublicId, It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.UploadImageAsync(mockFile, "fishfarms", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFishFarmPictureHandler_FarmNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler  = new UpdateFishFarmPictureCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var mockFile = TestDataFactory.CreateMockFormFile();

        // Act
        var act = async () => await handler.Handle(
            new UpdateFishFarmPictureCommand(missing, new UpdateFishFarmPictureRequest { Picture = mockFile }),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DeleteFishFarmCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteFishFarmHandler_ExistingFarm_SoftDeletesFarmAndWorkers()
    {
        // Arrange
        var farmId = TestDataFactory.FarmId1;
        var farm   = TestDataFactory.CreateFishFarmEntity(farmId, picturePublicId: "fishfarms/farm-pic");

        var activeWorker = TestDataFactory.CreateWorkerEntity(
            picturePublicId: "workers/worker-pic");
        farm.AddWorker(activeWorker);

        var workerRepoMock = new Mock<IWorkerRepository>();
        _uowMock.Setup(u => u.Workers).Returns(workerRepoMock.Object);

        _farmRepoMock
            .Setup(r => r.GetWithWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeleteFishFarmCommand(farmId), CancellationToken.None);

        // Assert – worker and farm both soft-deleted, Cloudinary assets cleaned up
        workerRepoMock.Verify(r => r.Delete(activeWorker), Times.Once);
        _farmRepoMock.Verify(r => r.Delete(farm), Times.Once);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync("workers/worker-pic", It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync("fishfarms/farm-pic", It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFishFarmHandler_FarmNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetWithWorkersAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new DeleteFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new DeleteFishFarmCommand(missing), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteFishFarmHandler_FarmWithNoActiveWorkers_OnlyFarmDeleted()
    {
        // Arrange
        // EF's global query filter guarantees GetWithWorkersAsync never returns soft-deleted
        // workers — the handler iterates farm.Workers unconditionally and relies on that contract.
        // This test models a farm that legitimately has no active workers.
        var farmId = TestDataFactory.FarmId1;
        var farm   = TestDataFactory.CreateFishFarmEntity(farmId);   // no workers added

        var workerRepoMock = new Mock<IWorkerRepository>();
        _uowMock.Setup(u => u.Workers).Returns(workerRepoMock.Object);

        _farmRepoMock
            .Setup(r => r.GetWithWorkersAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteFishFarmCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeleteFishFarmCommand(farmId), CancellationToken.None);

        // Assert – no workers to process; only the farm itself should be soft-deleted
        workerRepoMock.Verify(r => r.Delete(It.IsAny<Domain.Entities.Worker>()), Times.Never);
        _farmRepoMock.Verify(r => r.Delete(farm), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
