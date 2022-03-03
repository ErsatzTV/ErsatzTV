using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Application.Filler;

internal static class Mapper
{
    internal static FillerPresetViewModel ProjectToViewModel(FillerPreset fillerPreset) =>
        new(
            fillerPreset.Id,
            fillerPreset.Name,
            fillerPreset.FillerKind,
            fillerPreset.FillerMode,
            fillerPreset.Duration,
            fillerPreset.Count,
            fillerPreset.PadToNearestMinute,
            fillerPreset.CollectionType,
            fillerPreset.CollectionId,
            fillerPreset.MediaItemId,
            fillerPreset.MultiCollectionId,
            fillerPreset.SmartCollectionId);
}