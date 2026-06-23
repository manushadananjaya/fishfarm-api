using FishFarm.Application.Features.FarmWorkers.Commands;
using FishFarm.Application.Features.FarmWorkers.Queries;
using FishFarm.Domain.Enums;
using FluentValidation;

namespace FishFarm.Application.Features.FarmWorkers.Validators;

public sealed class AssignPersonToFarmCommandValidator
    : AbstractValidator<AssignPersonToFarmCommand>
{
    public AssignPersonToFarmCommandValidator()
    {
        RuleFor(x => x.Request.PersonId)
            .NotEmpty().WithMessage("PersonId is required.");

        RuleFor(x => x.Request.Position)
            .IsInEnum().WithMessage(
                $"Position must be one of: {string.Join(", ", Enum.GetNames<WorkerPosition>())}.");
    }
}

public sealed class UpdateFarmWorkerCommandValidator : AbstractValidator<UpdateFarmWorkerCommand>
{
    public UpdateFarmWorkerCommandValidator()
    {
        RuleFor(x => x.Request.Position)
            .IsInEnum().WithMessage(
                $"Position must be one of: {string.Join(", ", Enum.GetNames<WorkerPosition>())}.");
    }
}

public sealed class GetFarmWorkersQueryValidator : AbstractValidator<GetFarmWorkersQuery>
{
    public GetFarmWorkersQueryValidator()
    {
        When(x => x.Search is not null, () =>
            RuleFor(x => x.Search!)
                .MaximumLength(200)
                .WithMessage("Search term must not exceed 200 characters."));
    }
}
