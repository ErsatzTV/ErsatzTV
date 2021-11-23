using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class WatermarkEditViewModelValidator : AbstractValidator<WatermarkEditViewModel>
    {
        public WatermarkEditViewModelValidator()
        {
            RuleFor(x => x.Name).NotEmpty();

            RuleFor(x => x.Image)
                .NotEmpty()
                .WithMessage("Watermark image is required!")
                .When(vm => vm.ImageSource == ChannelWatermarkImageSource.Custom);
            
            RuleFor(x => x.Width)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .When(
                    vm => vm.Mode != ChannelWatermarkMode.None &&
                          vm.Size == ChannelWatermarkSize.Scaled);

            RuleFor(x => x.HorizontalMargin)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(50)
                .When(vm => vm.Mode != ChannelWatermarkMode.None);

            RuleFor(x => x.VerticalMargin)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(50)
                .When(vm => vm.Mode != ChannelWatermarkMode.None);

            RuleFor(x => x.DurationSeconds)
                .GreaterThan(0)
                .LessThan(c => c.FrequencyMinutes * 60)
                .When(vm => vm.Mode != ChannelWatermarkMode.None);

            RuleFor(x => x.Opacity)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .When(vm => vm.Mode != ChannelWatermarkMode.None);
        }
    }
}
