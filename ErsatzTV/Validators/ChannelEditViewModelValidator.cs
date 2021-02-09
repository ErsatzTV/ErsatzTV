using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class ChannelEditViewModelValidator : AbstractValidator<ChannelEditViewModel>
    {
        public ChannelEditViewModelValidator()
        {
            RuleFor(x => x.Number).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.FFmpegProfileId).GreaterThan(0);
        }
    }
}
