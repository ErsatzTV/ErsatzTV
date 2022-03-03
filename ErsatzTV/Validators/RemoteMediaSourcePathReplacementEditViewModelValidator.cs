using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class
    RemoteMediaSourcePathReplacementEditViewModelValidator : AbstractValidator<
        RemoteMediaSourcePathReplacementEditViewModel>
{
    public RemoteMediaSourcePathReplacementEditViewModelValidator()
    {
        RuleFor(vm => vm.RemotePath).NotEmpty();
        RuleFor(vm => vm.LocalPath).NotEmpty();
    }
}