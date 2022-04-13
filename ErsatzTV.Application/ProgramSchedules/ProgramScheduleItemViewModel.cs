using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules;

public abstract record ProgramScheduleItemViewModel(
    int Id,
    int Index,
    StartType StartType,
    TimeSpan? StartTime,
    PlayoutMode PlayoutMode,
    ProgramScheduleItemCollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    NamedMediaItemViewModel MediaItem,
    PlaybackOrder PlaybackOrder,
    string CustomTitle,
    GuideMode GuideMode,
    FillerPresetViewModel PreRollFiller,
    FillerPresetViewModel MidRollFiller,
    FillerPresetViewModel PostRollFiller,
    FillerPresetViewModel TailFiller,
    FillerPresetViewModel FallbackFiller,
    WatermarkViewModel Watermark)
{
    public string Name => CollectionType switch
    {
        ProgramScheduleItemCollectionType.Collection => Collection?.Name,
        ProgramScheduleItemCollectionType.TelevisionShow =>
            MediaItem?.Name, // $"{TelevisionShow?.Title} ({TelevisionShow?.Year})",
        ProgramScheduleItemCollectionType.TelevisionSeason =>
            MediaItem?.Name, // $"{TelevisionSeason?.Title} ({TelevisionSeason?.Plot})",
        ProgramScheduleItemCollectionType.Artist =>
            MediaItem?.Name,
        ProgramScheduleItemCollectionType.MultiCollection =>
            MultiCollection?.Name,
        ProgramScheduleItemCollectionType.SmartCollection =>
            SmartCollection?.Name,
        _ => string.Empty
    };
}