using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class LocalLibraryPathEditViewModelValidator : AbstractValidator<LocalLibraryPathEditViewModel>
{
    public LocalLibraryPathEditViewModelValidator()
    {
        RuleFor(x => x.Path).NotEmpty();
        RuleFor(x => x.Path).Must(Directory.Exists).WithMessage("Path must exist on filesystem");
    }
}
