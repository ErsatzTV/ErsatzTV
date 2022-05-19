using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class RemoteMediaSourceEditViewModelValidator : AbstractValidator<RemoteMediaSourceEditViewModel>
{
    public RemoteMediaSourceEditViewModelValidator()
    {
        RuleFor(x => x.Address)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("'Address' must be a valid URL");

        RuleFor(x => x.ApiKey)
            .NotEmpty();
    }
}
