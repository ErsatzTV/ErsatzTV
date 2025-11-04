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
    CollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    RerunCollectionViewModel RerunCollection,
    PlaylistViewModel Playlist,
    NamedMediaItemViewModel MediaItem,
    string SearchTitle,
    string SearchQuery,
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
        CollectionType.Collection => Collection?.Name,
        CollectionType.TelevisionShow =>
            MediaItem?.Name, // $"{TelevisionShow?.Title} ({TelevisionShow?.Year})",
        CollectionType.TelevisionSeason =>
            MediaItem?.Name, // $"{TelevisionSeason?.Title} ({TelevisionSeason?.Plot})",
        CollectionType.Artist =>
            MediaItem?.Name,
        CollectionType.MultiCollection =>
            MultiCollection?.Name,
        CollectionType.SmartCollection =>
            SmartCollection?.Name,
        CollectionType.SearchQuery =>
            string.IsNullOrWhiteSpace(SearchTitle) ? SearchQuery : SearchTitle,
        CollectionType.Playlist =>
            Playlist?.Name,
        CollectionType.RerunFirstRun or CollectionType.RerunRerun =>
            RerunCollection?.Name,
        _ => string.Empty
    };
}
