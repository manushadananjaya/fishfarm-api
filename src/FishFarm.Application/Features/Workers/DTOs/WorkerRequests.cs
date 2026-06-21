using FishFarm.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Features.Workers.DTOs;

public sealed class CreateWorkerRequest
{
    public string Name { get; init; } = default!;
    public int Age { get; init; }
    public string Email { get; init; } = default!;

    /// <summary>
    /// Accept either int value (1,2,3) or string name (CEO, Worker, Captain).
    /// Stored as INT in the database.
    /// </summary>
    public WorkerPosition Position { get; init; }

    public DateOnly CertifiedUntil { get; init; }

    /// <summary>Optional worker profile picture.</summary>
    public IFormFile? Picture { get; init; }
}

public sealed class UpdateWorkerRequest
{
    public string Name { get; init; } = default!;
    public int Age { get; init; }
    public string Email { get; init; } = default!;
    public WorkerPosition Position { get; init; }
    public DateOnly CertifiedUntil { get; init; }
}

public sealed class UpdateWorkerPictureRequest
{
    public IFormFile Picture { get; init; } = default!;
}
