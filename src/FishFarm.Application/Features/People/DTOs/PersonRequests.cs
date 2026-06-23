using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Features.People.DTOs;

public sealed class CreatePersonRequest
{
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public int Age { get; init; }
    public DateOnly CertifiedUntil { get; init; }
    public IFormFile? Picture { get; init; }
}

public sealed class UpdatePersonRequest
{
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public int Age { get; init; }
    public DateOnly CertifiedUntil { get; init; }
}

public sealed class UpdatePersonPictureRequest
{
    public IFormFile Picture { get; init; } = default!;
}
