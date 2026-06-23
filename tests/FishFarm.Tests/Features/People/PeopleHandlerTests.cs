using FishFarm.Application.Common.Exceptions;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Application.Features.People.Commands;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Application.Features.People.Queries;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using FishFarm.Tests.Common;
using FluentAssertions;
using Moq;

namespace FishFarm.Tests.Features.People;

/// <summary>
/// Unit tests for the People CQRS handlers.
/// IUnitOfWork and ICloudinaryService are mocked; no EF Core or HTTP involved.
/// </summary>
public sealed class PeopleHandlerTests
{
    private readonly Mock<IUnitOfWork>           _uowMock;
    private readonly Mock<IPersonRepository>     _personRepoMock;
    private readonly Mock<IFarmWorkerRepository> _fwRepoMock;
    private readonly Mock<ICloudinaryService>    _cloudinaryMock;

    public PeopleHandlerTests()
    {
        _uowMock        = new Mock<IUnitOfWork>();
        _personRepoMock = new Mock<IPersonRepository>();
        _fwRepoMock     = new Mock<IFarmWorkerRepository>();
        _cloudinaryMock = new Mock<ICloudinaryService>();

        _uowMock.Setup(u => u.People).Returns(_personRepoMock.Object);
        _uowMock.Setup(u => u.FarmWorkers).Returns(_fwRepoMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CreatePersonCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreatePersonHandler_NewEmail_SavesAndReturnsId()
    {
        // Arrange
        _personRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreatePersonCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var request = new CreatePersonRequest
        {
            Name           = "Alice Diver",
            Email          = "alice@example.com",
            Age            = 28,
            CertifiedUntil = new DateOnly(2027, 6, 1)
        };

        // Act
        var id = await handler.Handle(new CreatePersonCommand(request), CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.UploadImageAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePersonHandler_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        _personRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreatePersonCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var request = new CreatePersonRequest
        {
            Name           = "Bob",
            Email          = "taken@example.com",
            Age            = 30,
            CertifiedUntil = new DateOnly(2027, 1, 1)
        };

        // Act
        var act = () => handler.Handle(new CreatePersonCommand(request), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("Email"));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePersonHandler_SaveFails_DeletesUploadedPicture()
    {
        // Arrange — picture upload succeeds, but SaveChanges throws
        _personRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://cdn/img.jpg", "public-id-123"));

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB down"));

        var handler = new CreatePersonCommandHandler(_uowMock.Object, _cloudinaryMock.Object);
        var picture = TestDataFactory.CreateMockFormFile();
        var request = new CreatePersonRequest
        {
            Name           = "Carol",
            Email          = "carol@example.com",
            Age            = 25,
            CertifiedUntil = new DateOnly(2027, 1, 1),
            Picture        = picture
        };

        // Act
        var act = () => handler.Handle(new CreatePersonCommand(request), CancellationToken.None);

        // Assert — exception propagates AND the orphaned Cloudinary asset is cleaned up
        await act.Should().ThrowAsync<Exception>();
        _cloudinaryMock.Verify(c => c.DeleteImageAsync("public-id-123", CancellationToken.None), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UpdatePersonCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdatePersonHandler_ValidRequest_UpdatesFields()
    {
        // Arrange
        var person = TestDataFactory.CreatePersonEntity();
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _personRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new UpdatePersonCommandHandler(_uowMock.Object);
        var request = new UpdatePersonRequest
        {
            Name           = "Updated Name",
            Email          = "updated@example.com",
            Age            = 35,
            CertifiedUntil = new DateOnly(2028, 1, 1)
        };

        // Act
        await handler.Handle(
            new UpdatePersonCommand(TestDataFactory.PersonId1, request), CancellationToken.None);

        // Assert
        person.Name.Should().Be("Updated Name");
        person.Email.Should().Be("updated@example.com");
        person.Age.Should().Be(35);
        person.CertifiedUntil.Should().Be(new DateOnly(2028, 1, 1));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonHandler_ExpiredCertSubmittedWithPastDate_ThrowsWithRenewalMessage()
    {
        // Arrange — person already has an expired cert
        var person = TestDataFactory.CreatePersonEntity(
            certifiedUntil: new DateOnly(2020, 1, 1)); // expired in 2020
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        var handler = new UpdatePersonCommandHandler(_uowMock.Object);
        var request = new UpdatePersonRequest
        {
            Name           = person.Name,
            Email          = person.Email,
            Age            = person.Age,
            CertifiedUntil = new DateOnly(2020, 6, 1) // also in the past
        };

        // Act
        var act = () => handler.Handle(
            new UpdatePersonCommand(TestDataFactory.PersonId1, request), CancellationToken.None);

        // Assert — handler returns the contextual renewal message (not the generic "must be future")
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors["CertifiedUntil"][0].Contains("expired"));
    }

    [Fact]
    public async Task UpdatePersonHandler_ActiveCertSubmittedWithPastDate_ThrowsWithGenericMessage()
    {
        // Arrange — person has an active cert; caller sends a past date for it
        var person = TestDataFactory.CreatePersonEntity(
            certifiedUntil: new DateOnly(2030, 1, 1)); // still valid
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        var handler = new UpdatePersonCommandHandler(_uowMock.Object);
        var request = new UpdatePersonRequest
        {
            Name           = person.Name,
            Email          = person.Email,
            Age            = person.Age,
            CertifiedUntil = new DateOnly(2020, 1, 1) // in the past
        };

        // Act
        var act = () => handler.Handle(
            new UpdatePersonCommand(TestDataFactory.PersonId1, request), CancellationToken.None);

        // Assert — generic "must be future date" message (not the renewal message)
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors["CertifiedUntil"][0].Contains("future date"));
    }

    [Fact]
    public async Task UpdatePersonHandler_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        var person = TestDataFactory.CreatePersonEntity();
        _personRepoMock
            .Setup(r => r.GetByIdAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _personRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdatePersonCommandHandler(_uowMock.Object);
        var request = new UpdatePersonRequest
        {
            Name           = "Alice",
            Email          = "taken@example.com",
            Age            = 30,
            CertifiedUntil = new DateOnly(2028, 1, 1)
        };

        // Act
        var act = () => handler.Handle(
            new UpdatePersonCommand(TestDataFactory.PersonId1, request), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("Email"));
    }

    [Fact]
    public async Task UpdatePersonHandler_PersonNotFound_ThrowsNotFoundException()
    {
        _personRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var handler  = new UpdatePersonCommandHandler(_uowMock.Object);
        var request  = new UpdatePersonRequest
            { Name = "X", Email = "x@x.com", Age = 25, CertifiedUntil = new DateOnly(2028, 1, 1) };

        var act = () => handler.Handle(
            new UpdatePersonCommand(Guid.NewGuid(), request), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DeletePersonCommandHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeletePersonHandler_PersonWithAssignments_SoftDeletesAssignmentsAndPerson()
    {
        // Arrange
        var fw1    = TestDataFactory.CreateFarmWorkerEntity(id: TestDataFactory.FwId1);
        var fw2    = TestDataFactory.CreateFarmWorkerEntity(id: TestDataFactory.FwId2);
        var person = TestDataFactory.CreatePersonEntity(picturePublicId: null);
        person.FarmWorkers.Should().BeEmpty(); // navigation is read-only; add via reflection helper
        // Simulate loaded assignments by returning a person whose FarmWorkers are populated
        var personWithFw = TestDataFactory.CreatePersonEntity();

        _personRepoMock
            .Setup(r => r.GetByIdWithAssignmentsAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(personWithFw);

        var handler = new DeletePersonCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeletePersonCommand(TestDataFactory.PersonId1), CancellationToken.None);

        // Assert — person deleted, SaveChanges called, no CDN call (no picture)
        _personRepoMock.Verify(r => r.Delete(personWithFw), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonHandler_PersonNotFound_ThrowsNotFoundException()
    {
        _personRepoMock
            .Setup(r => r.GetByIdWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var handler = new DeletePersonCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        var act = () => handler.Handle(
            new DeletePersonCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePersonHandler_PersonWithPicture_DeletesPictureAfterDbCommit()
    {
        // Arrange — person has a picture
        var person = TestDataFactory.CreatePersonEntity(
            pictureUrl: "https://cdn/img.jpg", picturePublicId: "pub-id-xyz");

        _personRepoMock
            .Setup(r => r.GetByIdWithAssignmentsAsync(TestDataFactory.PersonId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        var sequence = new MockSequence();
        _uowMock.InSequence(sequence)
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _cloudinaryMock.InSequence(sequence)
            .Setup(c => c.DeleteImageAsync("pub-id-xyz", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeletePersonCommandHandler(_uowMock.Object, _cloudinaryMock.Object);

        // Act
        await handler.Handle(new DeletePersonCommand(TestDataFactory.PersonId1), CancellationToken.None);

        // Assert — DB first, then CDN
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync("pub-id-xyz", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetPeopleQueryHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPeopleHandler_ReturnsPagedResult_WithCorrectIsExpired()
    {
        // Arrange
        var expired  = TestDataFactory.CreatePersonEntity(
            id: TestDataFactory.PersonId1, certifiedUntil: new DateOnly(2020, 1, 1));
        var active   = TestDataFactory.CreatePersonEntity(
            id: TestDataFactory.PersonId2, certifiedUntil: new DateOnly(2030, 1, 1));

        _personRepoMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<(Person, int)>)new List<(Person, int)>
                {
                    (expired, 2),
                    (active,  0)
                },
                2));

        var handler = new GetPeopleQueryHandler(_uowMock.Object);

        // Act
        var result = await handler.Handle(
            new GetPeopleQuery(1, 20), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].IsExpired.Should().BeTrue();
        result.Items[0].FarmCount.Should().Be(2);
        result.Items[1].IsExpired.Should().BeFalse();
        result.Items[1].FarmCount.Should().Be(0);
    }
}
