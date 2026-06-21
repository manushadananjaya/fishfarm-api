using FishFarm.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Features.Workers.DTOs;

public sealed class CreateWorkerRequest
{
    public string Name { get; init; } = default!;
    public int Age { get; init; }
    public string Email { get; init; } = default!;
    public WorkerPosition Position { get; init; }

    public DateOnly CertifiedUntil { get; init; }

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
