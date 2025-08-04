using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;
using FluentValidation.Results;

namespace ErsatzTV.Validators;

public class ChannelEditViewModelValidator : AbstractValidator<ChannelEditViewModel>
{
    public ChannelEditViewModelValidator()
    {
        RuleFor(x => x.Number).Matches(Channel.NumberValidator)
            .WithMessage("Invalid channel number; two decimals are allowed for subchannels");

        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Group).NotEmpty();
        RuleFor(x => x.FFmpegProfileId).GreaterThan(0);

        When(
            x => !string.IsNullOrWhiteSpace(x.ExternalLogoUrl),
            () =>
            {
                RuleFor(x => x.ExternalLogoUrl)
                    .Must(Artwork.IsExternalUrl)
                    .WithMessage("External logo url is invalid");
            });

        When(
            x => x.IsEnabled == false,
            () =>
            {
                RuleFor(x => x.ShowInEpg)
                    .Must(x => x == false)
                    .WithMessage("Disabled channels cannot be shown in EPG");
            });
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        ValidationResult result = await ValidateAsync(
            ValidationContext<ChannelEditViewModel>.CreateWithOptions(
                (ChannelEditViewModel)model,
                x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
        {
            return [];
        }

        return result.Errors.Select(e => e.ErrorMessage);
    };
}
