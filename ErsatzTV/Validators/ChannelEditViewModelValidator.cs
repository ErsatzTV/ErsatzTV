using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class ChannelEditViewModelValidator : AbstractValidator<ChannelEditViewModel>
    {
        public ChannelEditViewModelValidator()
        {
            RuleFor(x => x.Number).Matches(Channel.NumberValidator)
                .WithMessage("Invalid channel number; one decimal is allowed for subchannels");

            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.FFmpegProfileId).GreaterThan(0);
            RuleFor(x => x.PreferredLanguageCode).Length(3)
                .When(vm => !string.IsNullOrWhiteSpace(vm.PreferredLanguageCode));
        }
    }
}
