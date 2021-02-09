using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class SimpleMediaCollectionEditViewModelValidator : AbstractValidator<SimpleMediaCollectionEditViewModel>
    {
        public SimpleMediaCollectionEditViewModelValidator() => RuleFor(c => c.Name).NotEmpty();
    }
}
