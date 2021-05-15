using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class JellyfinPathReplacementEditViewModelValidator : AbstractValidator<JellyfinPathReplacementEditViewModel>
    {
        public JellyfinPathReplacementEditViewModelValidator()
        {
            RuleFor(vm => vm.JellyfinPath).NotEmpty();
            RuleFor(vm => vm.LocalPath).NotEmpty();
        }
    }
}
