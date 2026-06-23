using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FishFarm.Tests.Common;

/// <summary>
/// Central factory for creating realistic test data. All IDs are fixed so
/// tests are deterministic and easy to read.
/// </summary>
public static class TestDataFactory
{
    // ── Well-known IDs ─────────────────────────────────────────────────────────

    public static readonly Guid FarmId1     = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid FarmId2     = new("22222222-0000-0000-0000-000000000002");
    public static readonly Guid PersonId1   = new("aaaa1111-0000-0000-0000-000000000001");
    public static readonly Guid PersonId2   = new("aaaa2222-0000-0000-0000-000000000002");
    public static readonly Guid FwId1       = new("bbbb1111-0000-0000-0000-000000000001");
    public static readonly Guid FwId2       = new("bbbb2222-0000-0000-0000-000000000002");

    // ── FishFarm Domain Entities ───────────────────────────────────────────────

    public static Domain.Entities.FishFarm CreateFishFarmEntity(
        Guid? id = null,
        string name = "Atlantic Salmon Farm",
        decimal lat = 60.3913m,
        decimal lng = 5.3221m,
        int cages = 12,
        bool hasBarge = true,
        string? pictureUrl = null,
        string? picturePublicId = null) => new()
    {
        Id               = id ?? FarmId1,
        Name             = name,
        GpsLatitude      = lat,
        GpsLongitude     = lng,
        NumberOfCages    = cages,
        HasBarge         = hasBarge,
        PictureUrl       = pictureUrl,
        PicturePublicId  = picturePublicId,
        CreatedAt        = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt        = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public static Person CreatePersonEntity(
        Guid? id = null,
        string name = "John Fisher",
        int age = 30,
        string email = "john.fisher@example.com",
        DateOnly? certifiedUntil = null,
        string? pictureUrl = null,
        string? picturePublicId = null) => new()
    {
        Id               = id ?? PersonId1,
        Name             = name,
        Age              = age,
        Email            = email,
        CertifiedUntil   = certifiedUntil ?? new DateOnly(2026, 12, 31),
        PictureUrl       = pictureUrl,
        PicturePublicId  = picturePublicId,
        CreatedAt        = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt        = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public static FarmWorker CreateFarmWorkerEntity(
        Guid? id = null,
        Guid? fishFarmId = null,
        Guid? personId = null,
        WorkerPosition position = WorkerPosition.Worker,
        Person? person = null) => new()
    {
        Id         = id ?? FwId1,
        FishFarmId = fishFarmId ?? FarmId1,
        PersonId   = personId ?? PersonId1,
        Position   = position,
        Person     = person ?? CreatePersonEntity(personId),
        CreatedAt  = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt  = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // ── FishFarm DTOs ──────────────────────────────────────────────────────────

    public static FishFarmSummaryDto CreateFishFarmSummaryDto(
        Guid? id = null,
        string name = "Atlantic Salmon Farm",
        int workerCount = 3) => new()
    {
        Id            = id ?? FarmId1,
        FarmCode      = "FF-00001",
        Name          = name,
        GpsLatitude   = 60.3913m,
        GpsLongitude  = 5.3221m,
        NumberOfCages = 12,
        HasBarge      = true,
        PictureUrl    = null,
        WorkerCount   = workerCount
    };

    public static FishFarmDto CreateFishFarmDto(Guid? id = null) => new()
    {
        Id            = id ?? FarmId1,
        FarmCode      = "FF-00001",
        Name          = "Atlantic Salmon Farm",
        GpsLatitude   = 60.3913m,
        GpsLongitude  = 5.3221m,
        NumberOfCages = 12,
        HasBarge      = true,
        PictureUrl    = null,
        CreatedAt     = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt     = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Workers       = []
    };

    // ── FishFarm Requests ──────────────────────────────────────────────────────

    public static CreateFishFarmRequest CreateFishFarmRequest(string name = "New Farm") => new()
    {
        Name          = name,
        GpsLatitude   = 60.0m,
        GpsLongitude  = 5.0m,
        NumberOfCages = 8,
        HasBarge      = false,
        Picture       = null
    };

    public static UpdateFishFarmRequest UpdateFishFarmRequest(string name = "Updated Farm") => new()
    {
        Name          = name,
        GpsLatitude   = 61.0m,
        GpsLongitude  = 6.0m,
        NumberOfCages = 10,
        HasBarge      = true
    };

    // ── FarmWorker DTOs ────────────────────────────────────────────────────────

    public static FarmWorkerDto CreateFarmWorkerDto(Guid? id = null, Guid? fishFarmId = null) => new()
    {
        Id             = id ?? FwId1,
        FishFarmId     = fishFarmId ?? FarmId1,
        PersonId       = PersonId1,
        PersonCode     = "P-00001",
        PersonName     = "John Fisher",
        PersonEmail    = "john.fisher@example.com",
        PersonAge      = 30,
        CertifiedUntil = new DateOnly(2026, 12, 31),
        Position       = WorkerPosition.Worker.ToString(),
        PictureUrl     = null,
        CreatedAt      = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt      = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // ── PaginatedResult helpers ────────────────────────────────────────────────

    public static PaginatedResult<FishFarmSummaryDto> CreatePaginatedResult(
        IReadOnlyList<FishFarmSummaryDto>? items = null,
        int totalCount = 2,
        int pageNumber = 1,
        int pageSize = 10)
    {
        items ??=
        [
            CreateFishFarmSummaryDto(FarmId1, "Farm 1"),
            CreateFishFarmSummaryDto(FarmId2, "Farm 2")
        ];
        return PaginatedResult<FishFarmSummaryDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    // ── IFormFile mock ─────────────────────────────────────────────────────────

    public static IFormFile CreateMockFormFile(
        string fileName    = "test-image.jpg",
        string contentType = "image/jpeg",
        long length        = 1024)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[length]));
        return mock.Object;
    }
}
