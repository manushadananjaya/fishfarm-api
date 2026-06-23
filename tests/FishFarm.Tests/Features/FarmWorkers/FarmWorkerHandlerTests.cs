using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Features.FarmWorkers.Commands;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using FishFarm.Domain.Interfaces;
using FishFarm.Tests.Common;
using FluentAssertions;
using Moq;

namespace FishFarm.Tests.Features.FarmWorkers;

/// <summary>
/// Unit tests for the FarmWorker CQRS handlers.
/// All dependencies are mocked — no EF Core or HTTP involved.
/// </summary>
public sealed class FarmWorkerHandlerTests
{
    private readonly Mock<IUnitOfWork>           _uowMock;
    private readonly Mock<IFishFarmRepository>   _farmRepoMock;
    private readonly Mock<IPersonRepository>     _personRepoMock;
    private readonly Mock<IFarmWorkerRepository> _fwRepoMock;

    public FarmWorkerHandlerTests()
    {
        _uowMock        = new Mock<IUnitOfWork>();
        _farmRepoMock   = new Mock<IFishFarmRepository>();
        _personRepoMock = new Mock<IPersonRepository>();
        _fwRepoMock     = new Mock<IFarmWorkerRepository>();

        _uowMock.Setup(u => u.FishFarms).Returns(_farmRepoMock.Object);
        _uowMock.Setup(u => u.People).Returns(_personRepoMock.Object);
        _uowMock.Setup(u => u.FarmWorkers).Returns(_fwRepoMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  AssignPersonToFarmCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AssignPersonHandler_ValidRequest_CreatesAssignmentAndReturnsId()
    {
        // Arrange
        var farm   = TestDataFactory.CreateFishFarmEntity();
        var person = TestDataFactory.CreatePersonEntity();

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _fwRepoMock
            .Setup(r => r.IsPersonAssignedAsync(
                TestDataFactory.FarmId1, TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _fwRepoMock
            .Setup(r => r.HasCeoAsync(
                TestDataFactory.FarmId1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new AssignPersonToFarmCommandHandler(_uowMock.Object);
        var request = new AssignPersonToFarmRequest
        {
            PersonId = TestDataFactory.PersonId1,
            Position = WorkerPosition.Worker
        };

        // Act
        var id = await handler.Handle(
            new AssignPersonToFarmCommand(TestDataFactory.FarmId1, request),
            CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        _fwRepoMock.Verify(r => r.AddAsync(It.IsAny<FarmWorker>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignPersonHandler_PersonAlreadyAssigned_ThrowsValidationException()
    {
        // Arrange
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmEntity());
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreatePersonEntity());
        _fwRepoMock
            .Setup(r => r.IsPersonAssignedAsync(
                TestDataFactory.FarmId1, TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // ← already assigned

        var handler = new AssignPersonToFarmCommandHandler(_uowMock.Object);
        var request = new AssignPersonToFarmRequest
            { PersonId = TestDataFactory.PersonId1, Position = WorkerPosition.Worker };

        // Act
        var act = () => handler.Handle(
            new AssignPersonToFarmCommand(TestDataFactory.FarmId1, request),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("PersonId"));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignPersonHandler_SecondCeoAttempt_ThrowsValidationException()
    {
        // Arrange
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmEntity());
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreatePersonEntity());
        _fwRepoMock
            .Setup(r => r.IsPersonAssignedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _fwRepoMock
            .Setup(r => r.HasCeoAsync(TestDataFactory.FarmId1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // ← farm already has a CEO

        var handler = new AssignPersonToFarmCommandHandler(_uowMock.Object);
        var request = new AssignPersonToFarmRequest
            { PersonId = TestDataFactory.PersonId1, Position = WorkerPosition.CEO };

        // Act
        var act = () => handler.Handle(
            new AssignPersonToFarmCommand(TestDataFactory.FarmId1, request),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors["Position"][0].Contains("CEO"));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignPersonHandler_ExpiredCertification_ThrowsValidationException()
    {
        // Arrange — person has an expired certificate
        var expiredPerson = TestDataFactory.CreatePersonEntity(
            certifiedUntil: new DateOnly(2020, 1, 1));

        _farmRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateFishFarmEntity());
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredPerson);
        _fwRepoMock
            .Setup(r => r.IsPersonAssignedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _fwRepoMock
            .Setup(r => r.HasCeoAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new AssignPersonToFarmCommandHandler(_uowMock.Object);
        var request = new AssignPersonToFarmRequest
            { PersonId = TestDataFactory.PersonId1, Position = WorkerPosition.Worker };

        // Act
        var act = () => handler.Handle(
            new AssignPersonToFarmCommand(TestDataFactory.FarmId1, request),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors["CertifiedUntil"][0].Contains("expired"));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignPersonHandler_FarmNotFound_ThrowsNotFoundException()
    {
        _farmRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.FishFarm?)null);

        var handler = new AssignPersonToFarmCommandHandler(_uowMock.Object);
        var request = new AssignPersonToFarmRequest
            { PersonId = TestDataFactory.PersonId1, Position = WorkerPosition.Worker };

        var act = () => handler.Handle(
            new AssignPersonToFarmCommand(TestDataFactory.FarmId1, request),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdateFarmWorkerCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateFarmWorkerHandler_ChangePosition_UpdatesAndSaves()
    {
        // Arrange
        var fw = TestDataFactory.CreateFarmWorkerEntity(position: WorkerPosition.Worker);
        _fwRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(
                TestDataFactory.FwId1, TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fw);
        _fwRepoMock
            .Setup(r => r.HasCeoAsync(
                TestDataFactory.FarmId1, TestDataFactory.FwId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new UpdateFarmWorkerCommandHandler(_uowMock.Object);
        var request = new UpdateFarmWorkerRequest { Position = WorkerPosition.CEO };

        // Act
        await handler.Handle(
            new UpdateFarmWorkerCommand(TestDataFactory.FarmId1, TestDataFactory.FwId1, request),
            CancellationToken.None);

        // Assert
        fw.Position.Should().Be(WorkerPosition.CEO);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFarmWorkerHandler_PromoteToCeoWhenOneExists_ThrowsValidationException()
    {
        // Arrange — another person is already CEO of this farm
        var fw = TestDataFactory.CreateFarmWorkerEntity(position: WorkerPosition.Worker);
        _fwRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(
                TestDataFactory.FwId1, TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fw);
        _fwRepoMock
            .Setup(r => r.HasCeoAsync(
                TestDataFactory.FarmId1, TestDataFactory.FwId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // ← already a CEO (excluding this record)

        var handler = new UpdateFarmWorkerCommandHandler(_uowMock.Object);
        var request = new UpdateFarmWorkerRequest { Position = WorkerPosition.CEO };

        // Act
        var act = () => handler.Handle(
            new UpdateFarmWorkerCommand(TestDataFactory.FarmId1, TestDataFactory.FwId1, request),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors["Position"][0].Contains("CEO"));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFarmWorkerHandler_AssignmentNotFound_ThrowsNotFoundException()
    {
        _fwRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FarmWorker?)null);

        var handler = new UpdateFarmWorkerCommandHandler(_uowMock.Object);
        var request = new UpdateFarmWorkerRequest { Position = WorkerPosition.Worker };

        var act = () => handler.Handle(
            new UpdateFarmWorkerCommand(TestDataFactory.FarmId1, TestDataFactory.FwId1, request),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  RemoveFarmWorkerCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RemoveFarmWorkerHandler_ExistingAssignment_SoftDeletesAndSaves()
    {
        // Arrange
        var fw = TestDataFactory.CreateFarmWorkerEntity();
        _fwRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(
                TestDataFactory.FwId1, TestDataFactory.FarmId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fw);

        var handler = new RemoveFarmWorkerCommandHandler(_uowMock.Object);

        // Act
        await handler.Handle(
            new RemoveFarmWorkerCommand(TestDataFactory.FarmId1, TestDataFactory.FwId1),
            CancellationToken.None);

        // Assert
        _fwRepoMock.Verify(r => r.Delete(fw), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFarmWorkerHandler_AssignmentNotFound_ThrowsNotFoundException()
    {
        _fwRepoMock
            .Setup(r => r.GetByIdAndFarmAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FarmWorker?)null);

        var handler = new RemoveFarmWorkerCommandHandler(_uowMock.Object);

        var act = () => handler.Handle(
            new RemoveFarmWorkerCommand(TestDataFactory.FarmId1, TestDataFactory.FwId1),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
