using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class PlexPathReplacementEditViewModelValidator : AbstractValidator<PlexPathReplacementEditViewModel>
{
    public PlexPathReplacementEditViewModelValidator()
    {
        RuleFor(vm => vm.PlexPath).NotEmpty();
        RuleFor(vm => vm.LocalPath).NotEmpty();
    }
}