using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class PlayoutAlternateScheduleEditViewModelValidator : AbstractValidator<PlayoutAlternateScheduleEditViewModel>
{
    public PlayoutAlternateScheduleEditViewModelValidator() => RuleFor(p => p.ProgramSchedule).NotNull();
}
