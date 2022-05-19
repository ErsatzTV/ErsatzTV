using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class MultiCollectionEditViewModelValidator : AbstractValidator<MultiCollectionEditViewModel>
{
    public MultiCollectionEditViewModelValidator() => RuleFor(c => c.Name).NotEmpty();
}
