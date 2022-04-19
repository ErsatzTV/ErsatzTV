using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class CollectionEditViewModelValidator : AbstractValidator<CollectionEditViewModel>
{
    public CollectionEditViewModelValidator() => RuleFor(c => c.Name).NotEmpty();
}
