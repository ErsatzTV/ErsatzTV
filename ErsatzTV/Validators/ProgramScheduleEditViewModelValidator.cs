using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class ProgramScheduleEditViewModelValidator : AbstractValidator<ProgramScheduleEditViewModel>
    {
        public ProgramScheduleEditViewModelValidator() => RuleFor(vm => vm.Name).NotEmpty();
    }
}
