using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class LocalLibraryEditViewModelValidator : AbstractValidator<LocalLibraryEditViewModel>
    {
        public LocalLibraryEditViewModelValidator() => RuleFor(c => c.Name).NotEmpty();
    }
}
