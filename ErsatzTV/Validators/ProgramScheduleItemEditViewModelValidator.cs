using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class ProgramScheduleItemEditViewModelValidator : AbstractValidator<ProgramScheduleItemEditViewModel>
{
    public ProgramScheduleItemEditViewModelValidator()
    {
        When(
            i => i.StartType == StartType.Fixed,
            () => RuleFor(i => i.StartTime).NotNull());
        When(
            i => i.PlayoutMode == PlayoutMode.Multiple,
            () => RuleFor(i => i.MultipleCount).NotNull().GreaterThanOrEqualTo(0));
        When(
            i => i.PlayoutMode == PlayoutMode.Duration,
            () => RuleFor(i => i.PlayoutDuration).NotNull());
    }
}
