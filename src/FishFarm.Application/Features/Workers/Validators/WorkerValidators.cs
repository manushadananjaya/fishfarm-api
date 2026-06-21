using FishFarm.Application.Features.Workers.Commands;
using FishFarm.Domain.Enums;
using FluentValidation;

namespace FishFarm.Application.Features.Workers.Validators;

public sealed class CreateWorkerCommandValidator : AbstractValidator<CreateWorkerCommand>
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 3 * 1024 * 1024; // 3 MB

    public CreateWorkerCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.Request.Age)
            .InclusiveBetween(18, 80).WithMessage("Age must be between 18 and 80.");

        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Request.Position)
            .IsInEnum().WithMessage(
                $"Position must be one of: {string.Join(", ", Enum.GetNames<WorkerPosition>())}.");

        RuleFor(x => x.Request.CertifiedUntil)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("CertifiedUntil must be a future date.");

        When(x => x.Request.Picture is not null, () =>
        {
            RuleFor(x => x.Request.Picture!.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("Picture must not exceed 3 MB.");

            RuleFor(x => x.Request.Picture!.ContentType)
                .Must(ct => AllowedImageTypes.Contains(ct))
                .WithMessage("Picture must be a JPEG, PNG, or WebP image.");
        });
    }
}

public sealed class UpdateWorkerCommandValidator : AbstractValidator<UpdateWorkerCommand>
{
    public UpdateWorkerCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.Request.Age)
            .InclusiveBetween(18, 80).WithMessage("Age must be between 18 and 80.");

        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Request.Position)
            .IsInEnum().WithMessage(
                $"Position must be one of: {string.Join(", ", Enum.GetNames<WorkerPosition>())}.");

        RuleFor(x => x.Request.CertifiedUntil)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("CertifiedUntil must be a future date.");
    }
}

public sealed class UpdateWorkerPictureCommandValidator
    : AbstractValidator<UpdateWorkerPictureCommand>
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 3 * 1024 * 1024; // 3 MB

    public UpdateWorkerPictureCommandValidator()
    {
        RuleFor(x => x.Request.Picture)
            .NotNull().WithMessage("A picture file is required.");

        When(x => x.Request.Picture is not null, () =>
        {
            RuleFor(x => x.Request.Picture.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("Picture must not exceed 3 MB.");

            RuleFor(x => x.Request.Picture.ContentType)
                .Must(ct => AllowedImageTypes.Contains(ct))
                .WithMessage("Picture must be a JPEG, PNG, or WebP image.");
        });
    }
}
