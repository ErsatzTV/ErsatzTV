using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.ViewModels;
using FluentValidation;

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
}
