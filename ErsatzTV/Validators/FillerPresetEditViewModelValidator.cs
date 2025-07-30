using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.ViewModels;
using FluentValidation;
using FluentValidation.Results;

namespace ErsatzTV.Validators;

public class FillerPresetEditViewModelValidator : AbstractValidator<FillerPresetEditViewModel>
{
    public FillerPresetEditViewModelValidator()
    {
        RuleFor(fp => fp.Name).NotEmpty();
        RuleFor(fp => fp.FillerKind).NotEqual(FillerKind.None);

        When(
            fp => fp.FillerKind != FillerKind.Fallback && fp.FillerKind != FillerKind.Tail,
            () => RuleFor(fp => fp.FillerMode).NotEqual(FillerMode.None));
        When(
            fp => fp.FillerMode == FillerMode.Count,
            () => RuleFor(fp => fp.Count).NotNull().GreaterThanOrEqualTo(0));
        When(
            fp => fp.FillerMode == FillerMode.Duration,
            () => RuleFor(fp => fp.Duration).NotNull());
        When(
            fp => fp.FillerMode == FillerMode.Pad,
            () => RuleFor(fp => fp.PadToNearestMinute).NotNull());
        When(
            fp => fp.FillerKind is FillerKind.Fallback,
            () => RuleFor(fp => fp.UseChaptersAsMediaItems).NotEqual(true));

        When(
            fp => fp.CollectionType == ProgramScheduleItemCollectionType.Collection,
            () => RuleFor(fp => fp.Collection).NotNull());
        When(
            fp => fp.CollectionType == ProgramScheduleItemCollectionType.MultiCollection,
            () => RuleFor(fp => fp.MultiCollection).NotNull());
        When(
            fp => fp.CollectionType == ProgramScheduleItemCollectionType.SmartCollection,
            () => RuleFor(fp => fp.SmartCollection).NotNull());
        When(
            fp => fp.CollectionType is ProgramScheduleItemCollectionType.Artist or ProgramScheduleItemCollectionType
                .TelevisionShow or ProgramScheduleItemCollectionType.TelevisionSeason,
            () => RuleFor(fp => fp.MediaItem).NotNull());
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        ValidationResult result = await ValidateAsync(
            ValidationContext<FillerPresetEditViewModel>.CreateWithOptions(
                (FillerPresetEditViewModel)model,
                x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
        {
            return [];
        }

        return result.Errors.Select(e => e.ErrorMessage);
    };
}
