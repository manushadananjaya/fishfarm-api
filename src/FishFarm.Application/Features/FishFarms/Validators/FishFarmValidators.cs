using FishFarm.Application.Features.FishFarms.Commands;
using FishFarm.Application.Features.FishFarms.Queries;
using FluentValidation;

namespace FishFarm.Application.Features.FishFarms.Validators;

public sealed class CreateFishFarmCommandValidator : AbstractValidator<CreateFishFarmCommand>
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public CreateFishFarmCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Request.GpsLatitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .Must(v => decimal.Round(v, 4) == v)
            .WithMessage("Latitude must have at most 4 decimal places.");

        RuleFor(x => x.Request.GpsLongitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .Must(v => decimal.Round(v, 4) == v)
            .WithMessage("Longitude must have at most 4 decimal places.");

        RuleFor(x => x.Request.NumberOfCages)
            .GreaterThan(0).WithMessage("NumberOfCages must be greater than 0.");

        // Picture is optional on create — farms can be registered without one and a picture
        // uploaded separately via PATCH /picture. See FishFarmRequests.cs for rationale.
        When(x => x.Request.Picture is not null, () =>
        {
            RuleFor(x => x.Request.Picture!.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("Picture must not exceed 5 MB.");

            RuleFor(x => x.Request.Picture!.ContentType)
                .Must(ct => AllowedImageTypes.Contains(ct))
                .WithMessage("Picture must be a JPEG, PNG, or WebP image.");
        });
    }
}

public sealed class UpdateFishFarmCommandValidator : AbstractValidator<UpdateFishFarmCommand>
{
    public UpdateFishFarmCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Request.GpsLatitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .Must(v => decimal.Round(v, 4) == v)
            .WithMessage("Latitude must have at most 4 decimal places.");

        RuleFor(x => x.Request.GpsLongitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .Must(v => decimal.Round(v, 4) == v)
            .WithMessage("Longitude must have at most 4 decimal places.");

        RuleFor(x => x.Request.NumberOfCages)
            .GreaterThan(0).WithMessage("NumberOfCages must be greater than 0.");
    }
}

public sealed class UpdateFishFarmPictureCommandValidator
    : AbstractValidator<UpdateFishFarmPictureCommand>
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UpdateFishFarmPictureCommandValidator()
    {
        RuleFor(x => x.Request.Picture)
            .NotNull().WithMessage("A picture file is required.");

        When(x => x.Request.Picture is not null, () =>
        {
            RuleFor(x => x.Request.Picture.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("Picture must not exceed 5 MB.");

            RuleFor(x => x.Request.Picture.ContentType)
                .Must(ct => AllowedImageTypes.Contains(ct))
                .WithMessage("Picture must be a JPEG, PNG, or WebP image.");
        });
    }
}

public sealed class GetFishFarmsQueryValidator : AbstractValidator<GetFishFarmsQuery>
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "name", "createdAt", "updatedAt", "numberOfCages", "workerCount"
        };

    public GetFishFarmsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageSize must be at least 1.");

        When(x => x.Search is not null, () =>
            RuleFor(x => x.Search!)
                .MaximumLength(200)
                .WithMessage("Search term must not exceed 200 characters."));

        When(x => x.MinCages.HasValue, () =>
            RuleFor(x => x.MinCages!.Value)
                .GreaterThan(0)
                .WithMessage("MinCages must be greater than 0."));

        When(x => x.MaxCages.HasValue, () =>
            RuleFor(x => x.MaxCages!.Value)
                .GreaterThan(0)
                .WithMessage("MaxCages must be greater than 0."));

        When(x => x.MinCages.HasValue && x.MaxCages.HasValue, () =>
            RuleFor(x => x.MinCages!.Value)
                .LessThanOrEqualTo(x => x.MaxCages!.Value)
                .WithMessage("MinCages must not be greater than MaxCages."));

        RuleFor(x => x.SortBy)
            .Must(v => AllowedSortFields.Contains(v))
            .WithMessage("SortBy must be one of: name, createdAt, updatedAt, numberOfCages, workerCount.");

        RuleFor(x => x.SortDir)
            .Must(v => v.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                       v.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir must be 'asc' or 'desc'.");
    }
}
