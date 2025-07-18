﻿using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;
using ErsatzTV.ViewModels;
using FluentValidation;
using FluentValidation.Results;

namespace ErsatzTV.Validators;

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
            .GreaterThan(0.0)
            .LessThanOrEqualTo(100.0)
            .When(vm => vm.Mode != ChannelWatermarkMode.None &&
                        vm.Size == WatermarkSize.Scaled);

        RuleFor(x => x.HorizontalMargin)
            .GreaterThanOrEqualTo(0.0)
            .LessThanOrEqualTo(50.0)
            .When(vm => vm.Mode != ChannelWatermarkMode.None);

        RuleFor(x => x.VerticalMargin)
            .GreaterThanOrEqualTo(0.0)
            .LessThanOrEqualTo(50.0)
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

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        ValidationResult result = await ValidateAsync(
            ValidationContext<WatermarkEditViewModel>.CreateWithOptions(
                (WatermarkEditViewModel)model,
                x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
        {
            return [];
        }

        return result.Errors.Select(e => e.ErrorMessage);
    };
}
