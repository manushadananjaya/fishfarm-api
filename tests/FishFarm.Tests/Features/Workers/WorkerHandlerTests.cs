using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.Workers.Commands;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Application.Features.Workers.Queries;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using FishFarm.Tests.Common;
using FluentAssertions;
using Moq;

namespace FishFarm.Tests.Features.Workers;

/// <summary>
/// Unit tests for the Worker CQRS handlers.
/// </summary>
public sealed class WorkerHandlerTests
{
    private readonly Mock<IUnitOfWork>                                       _uowMock;
    private readonly Mock<IFishFarmRepository>                               _farmRepoMock;
    private readonly Mock<IWorkerRepository>                                 _workerRepoMock;
    private readonly Mock<Application.Common.Interfaces.ICloudinaryService>  _cloudinaryMock;

    public WorkerHandlerTests()
    {
        _uowMock        = new Mock<IUnitOfWork>();
        _farmRepoMock   = new Mock<IFishFarmRepository>();
        _workerRepoMock = new Mock<IWorkerRepository>();
        _cloudinaryMock = new Mock<Application.Common.Interfaces.ICloudinaryService>();

        _uowMock.Setup(u => u.FishFarms).Returns(_farmRepoMock.Object);
        _uowMock.Setup(u => u.Workers).Returns(_workerRepoMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetWorkersByFarmQueryHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetWorkersByFarmHandler_ExistingFarm_ReturnsMappedDtos()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var farm    = TestDataFactory.CreateFishFarmEntity(farmId);
        var worker1 = TestDataFactory.CreateWorkerEntity(TestDataFactory.WorkerId1, farmId);
        var worker2 = TestDataFactory.CreateWorkerEntity(
            TestDataFactory.WorkerId2, farmId,
            name: "Jane Nets", email: "jane@example.com");

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _workerRepoMock
            .Setup(r => r.GetByFishFarmAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([worker1, worker2]);

        var handler = new GetWorkersByFarmQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetWorkersByFarmQuery(farmId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("John Fisher");
        result[1].Name.Should().Be("Jane Nets");
        result[0].FishFarmId.Should().Be(farmId);
        result[0].Position.Should().Be(WorkerPosition.Worker.ToString());
    }

    [Fact]
    public async Task GetWorkersByFarmHandler_FarmNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new GetWorkersByFarmQueryHandler(_uowMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new GetWorkersByFarmQuery(missing), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*FishFarm*{missing}*");
    }

    [Fact]
    public async Task GetWorkersByFarmHandler_NoWorkers_ReturnsEmptyList()
    {
        // Arrange
        var farmId = TestDataFactory.FarmId1;
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmEntity(farmId));
        _workerRepoMock
            .Setup(r => r.GetByFishFarmAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Worker>());

        var handler = new GetWorkersByFarmQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(new GetWorkersByFarmQuery(farmId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetWorkerByIdQueryHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetWorkerByIdHandler_ExistingWorker_ReturnsMappedDto()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        var worker   = TestDataFactory.CreateWorkerEntity(workerId, farmId);

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(workerId, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(worker);

        var handler = new GetWorkerByIdQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(
            new GetWorkerByIdQuery(farmId, workerId), CancellationToken.None);

        // Assert
        result.Id.Should().Be(workerId);
        result.FishFarmId.Should().Be(farmId);
        result.Name.Should().Be("John Fisher");
        result.Age.Should().Be(30);
        result.Email.Should().Be("john.fisher@example.com");
        result.Position.Should().Be(WorkerPosition.Worker.ToString());
    }

    [Fact]
    public async Task GetWorkerByIdHandler_WorkerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var missing  = Guid.NewGuid();

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(missing, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Worker?)null);

        var handler = new GetWorkerByIdQueryHandler(_uowMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new GetWorkerByIdQuery(farmId, missing), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Worker*{missing}*");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CreateWorkerCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateWorkerHandler_NoPicture_CreatesWorkerAndReturnsId()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var request = TestDataFactory.CreateWorkerRequest("Alice Marine");

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmEntity(farmId));
        _workerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Worker>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateWorkerCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var newId = await handler.Handle(new CreateWorkerCommand(farmId, request), CancellationToken.None);

        // Assert
        newId.Should().NotBeEmpty();
        _workerRepoMock.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.Worker>(w =>
                w.Name       == "Alice Marine" &&
                w.FishFarmId == farmId),
            It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.UploadImageAsync(
            It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateWorkerHandler_WithPicture_UploadsAndStoresUrl()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var mockFile = TestDataFactory.CreateMockFormFile("worker.jpg");
        const string cloudUrl = "https://res.cloudinary.com/demo/workers/worker.jpg";
        const string pubId    = "workers/worker";

        var request = new CreateWorkerRequest
        {
            Name           = "Bob Nets",
            Age            = 27,
            Email          = "bob@example.com",
            Position       = WorkerPosition.Captain,
            CertifiedUntil = new DateOnly(2028, 1, 1),
            Picture        = mockFile
        };

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmEntity(farmId));
        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(mockFile, "workers", It.IsAny<CancellationToken>()))
            .ReturnsAsync((cloudUrl, pubId));
        _workerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Worker>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateWorkerCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var newId = await handler.Handle(new CreateWorkerCommand(farmId, request), CancellationToken.None);

        // Assert
        newId.Should().NotBeEmpty();
        _workerRepoMock.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.Worker>(w =>
                w.PictureUrl      == cloudUrl &&
                w.PicturePublicId == pubId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWorkerHandler_FarmNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missing = Guid.NewGuid();
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new CreateWorkerCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new CreateWorkerCommand(missing, TestDataFactory.CreateWorkerRequest()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdateWorkerCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateWorkerHandler_ExistingWorker_UpdatesAndSaves()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        var worker   = TestDataFactory.CreateWorkerEntity(workerId, farmId);
        var request  = TestDataFactory.UpdateWorkerRequest("Updated Name");

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(workerId, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(worker);
        _workerRepoMock.Setup(r => r.Update(It.IsAny<Domain.Entities.Worker>()));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateWorkerCommandHandler(_uowMock.Object);

        // Act
        await handler.Handle(new UpdateWorkerCommand(farmId, workerId, request), CancellationToken.None);

        // Assert
        worker.Name.Should().Be("Updated Name");
        worker.Age.Should().Be(request.Age);
        worker.Email.Should().Be(request.Email);
        worker.Position.Should().Be(request.Position);
        _workerRepoMock.Verify(r => r.Update(worker), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWorkerHandler_WorkerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var missing = Guid.NewGuid();

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(missing, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Worker?)null);

        var handler = new UpdateWorkerCommandHandler(_uowMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new UpdateWorkerCommand(farmId, missing, TestDataFactory.UpdateWorkerRequest()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdateWorkerPictureCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateWorkerPictureHandler_DeletesOldAndUploadsNew()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        const string oldPubId  = "workers/old";
        const string newUrl    = "https://res.cloudinary.com/demo/workers/new.jpg";
        const string newPubId  = "workers/new";

        var worker   = TestDataFactory.CreateWorkerEntity(workerId, farmId, picturePublicId: oldPubId);
        var mockFile = TestDataFactory.CreateMockFormFile();
        var request  = new UpdateWorkerPictureRequest { Picture = mockFile };

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(workerId, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(worker);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(oldPubId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(mockFile, "workers", It.IsAny<CancellationToken>()))
            .ReturnsAsync((newUrl, newPubId));
        _workerRepoMock.Setup(r => r.Update(It.IsAny<Domain.Entities.Worker>()));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateWorkerPictureCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateWorkerPictureCommand(farmId, workerId, request), CancellationToken.None);

        // Assert
        result.Should().Be(newUrl);
        worker.PictureUrl.Should().Be(newUrl);
        worker.PicturePublicId.Should().Be(newPubId);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync(oldPubId, It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.UploadImageAsync(mockFile, "workers", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWorkerPictureHandler_WorkerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var missing = Guid.NewGuid();

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(missing, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Worker?)null);

        var handler  = new UpdateWorkerPictureCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var mockFile = TestDataFactory.CreateMockFormFile();

        // Act
        var act = async () => await handler.Handle(
            new UpdateWorkerPictureCommand(farmId, missing, new UpdateWorkerPictureRequest { Picture = mockFile }),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DeleteWorkerCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteWorkerHandler_ExistingWorker_SoftDeletesAndCleansCloudinary()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        const string picPubId = "workers/pic";

        var worker = TestDataFactory.CreateWorkerEntity(workerId, farmId, picturePublicId: picPubId);

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(workerId, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(worker);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(picPubId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteWorkerCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeleteWorkerCommand(farmId, workerId), CancellationToken.None);

        // Assert
        _workerRepoMock.Verify(r => r.Delete(worker), Times.Once);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync(picPubId, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWorkerHandler_NoPicture_StillSoftDeletes()
    {
        // Arrange
        var farmId   = TestDataFactory.FarmId1;
        var workerId = TestDataFactory.WorkerId1;
        var worker   = TestDataFactory.CreateWorkerEntity(workerId, farmId); // no picture

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(workerId, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(worker);
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteWorkerCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeleteWorkerCommand(farmId, workerId), CancellationToken.None);

        // Assert
        _workerRepoMock.Verify(r => r.Delete(worker), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWorkerHandler_WorkerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var farmId  = TestDataFactory.FarmId1;
        var missing = Guid.NewGuid();

        _workerRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(missing, farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Worker?)null);

        var handler = new DeleteWorkerCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new DeleteWorkerCommand(farmId, missing), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
