using ErsatzTV.Application.Filler;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public abstract record ProgramScheduleItemViewModel(
    int Id,
    int Index,
    StartType StartType,
    TimeSpan? StartTime,
    FixedStartTimeBehavior? FixedStartTimeBehavior,
    PlayoutMode PlayoutMode,
    ProgramScheduleItemCollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    PlaylistViewModel Playlist,
    NamedMediaItemViewModel MediaItem,
    PlaybackOrder PlaybackOrder,
    MarathonGroupBy MarathonGroupBy,
    bool MarathonShuffleGroups,
    bool MarathonShuffleItems,
    int? MarathonBatchSize,
    FillWithGroupMode FillWithGroupMode,
    string CustomTitle,
    GuideMode GuideMode,
    FillerPresetViewModel PreRollFiller,
    FillerPresetViewModel MidRollFiller,
    FillerPresetViewModel PostRollFiller,
    FillerPresetViewModel TailFiller,
    FillerPresetViewModel FallbackFiller,
    List<WatermarkViewModel> Watermarks,
    List<GraphicsElementViewModel> GraphicsElements,
    string PreferredAudioLanguageCode,
    string PreferredAudioTitle,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode)
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
        ProgramScheduleItemCollectionType.Playlist =>
            Playlist?.Name,
        _ => string.Empty
    };
}
