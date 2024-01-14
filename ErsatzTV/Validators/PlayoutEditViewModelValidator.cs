using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class PlayoutEditViewModelValidator : AbstractValidator<PlayoutEditViewModel>
{
    public PlayoutEditViewModelValidator()
    {
        RuleFor(p => p.Channel).NotNull();
        RuleFor(p => p.ProgramSchedule).NotNull().When(p => string.IsNullOrWhiteSpace(p.Kind));
        RuleFor(p => p.ExternalJsonFile).NotNull().When(p => p.Kind == PlayoutKind.ExternalJson);
    }
}
