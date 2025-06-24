using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;

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
    }
}
