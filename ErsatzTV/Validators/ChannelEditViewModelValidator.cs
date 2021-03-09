using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class ChannelEditViewModelValidator : AbstractValidator<ChannelEditViewModel>
    {
        public ChannelEditViewModelValidator()
        {
            RuleFor(x => x.Number).Matches(@"^[0-9]+(\.[0-9])?$")
                .WithMessage("Invalid channel number; one decimal is allowed for subchannels");

            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.FFmpegProfileId).GreaterThan(0);
        }
    }
}
