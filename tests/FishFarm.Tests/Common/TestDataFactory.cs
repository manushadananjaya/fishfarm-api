using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.Workers.DTOs;
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

    public static readonly Guid FarmId1   = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid FarmId2   = new("22222222-0000-0000-0000-000000000002");
    public static readonly Guid WorkerId1 = new("aaaa1111-0000-0000-0000-000000000001");
    public static readonly Guid WorkerId2 = new("aaaa2222-0000-0000-0000-000000000002");

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

    public static Domain.Entities.Worker CreateWorkerEntity(
        Guid? id = null,
        Guid? fishFarmId = null,
        string name = "John Fisher",
        int age = 30,
        string email = "john.fisher@example.com",
        WorkerPosition position = WorkerPosition.Worker,
        DateOnly? certifiedUntil = null,
        string? pictureUrl = null,
        string? picturePublicId = null) => new()
    {
        Id               = id ?? WorkerId1,
        FishFarmId       = fishFarmId ?? FarmId1,
        Name             = name,
        Age              = age,
        Email            = email,
        Position         = position,
        CertifiedUntil   = certifiedUntil ?? new DateOnly(2026, 12, 31),
        PictureUrl       = pictureUrl,
        PicturePublicId  = picturePublicId,
        CreatedAt        = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt        = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // ── FishFarm DTOs ──────────────────────────────────────────────────────────

    public static FishFarmSummaryDto CreateFishFarmSummaryDto(
        Guid? id = null,
        string name = "Atlantic Salmon Farm",
        int workerCount = 3) => new()
    {
        Id            = id ?? FarmId1,
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

    // ── Worker DTOs ────────────────────────────────────────────────────────────

    public static WorkerDto CreateWorkerDto(Guid? id = null, Guid? fishFarmId = null) => new()
    {
        Id             = id ?? WorkerId1,
        FishFarmId     = fishFarmId ?? FarmId1,
        Name           = "John Fisher",
        Age            = 30,
        Email          = "john.fisher@example.com",
        Position       = WorkerPosition.Worker.ToString(),
        CertifiedUntil = new DateOnly(2026, 12, 31),
        PictureUrl     = null,
        CreatedAt      = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt      = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // ── Worker Requests ────────────────────────────────────────────────────────

    public static CreateWorkerRequest CreateWorkerRequest(string name = "New Worker") => new()
    {
        Name           = name,
        Age            = 25,
        Email          = "new.worker@example.com",
        Position       = WorkerPosition.Worker,
        CertifiedUntil = new DateOnly(2027, 6, 30),
        Picture        = null
    };

    public static UpdateWorkerRequest UpdateWorkerRequest(string name = "Updated Worker") => new()
    {
        Name           = name,
        Age            = 32,
        Email          = "updated@example.com",
        Position       = WorkerPosition.Captain,
        CertifiedUntil = new DateOnly(2028, 1, 1)
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
