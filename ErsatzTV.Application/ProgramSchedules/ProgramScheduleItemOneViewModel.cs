using ErsatzTV.Application.Filler;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public record ProgramScheduleItemOneViewModel : ProgramScheduleItemViewModel
{
    public ProgramScheduleItemOneViewModel(
        int id,
        int index,
        StartType startType,
        TimeSpan? startTime,
        FixedStartTimeBehavior? fixedStartTimeBehavior,
        CollectionType collectionType,
        MediaCollectionViewModel collection,
        MultiCollectionViewModel multiCollection,
        SmartCollectionViewModel smartCollection,
        RerunCollectionViewModel rerunCollection,
        PlaylistViewModel playlist,
        NamedMediaItemViewModel mediaItem,
        string searchTitle,
        string searchQuery,
        PlaybackOrder playbackOrder,
        MarathonGroupBy marathonGroupBy,
        bool marathonShuffleGroups,
        bool marathonShuffleItems,
        int? marathonBatchSize,
        FillWithGroupMode fillWithGroupMode,
        string customTitle,
        GuideMode guideMode,
        FillerPresetViewModel preRollFiller,
        FillerPresetViewModel midRollFiller,
        FillerPresetViewModel postRollFiller,
        FillerPresetViewModel tailFiller,
        FillerPresetViewModel fallbackFiller,
        List<WatermarkViewModel> watermarks,
        List<GraphicsElementViewModel> graphicsElements,
        string preferredAudioLanguageCode,
        string preferredAudioTitle,
        string preferredSubtitleLanguageCode,
        ChannelSubtitleMode? subtitleMode) : base(
        id,
        index,
        startType,
        startTime,
        fixedStartTimeBehavior,
        PlayoutMode.One,
        collectionType,
        collection,
        multiCollection,
        smartCollection,
        rerunCollection,
        playlist,
        mediaItem,
        searchTitle,
        searchQuery,
        playbackOrder,
        marathonGroupBy,
        marathonShuffleGroups,
        marathonShuffleItems,
        marathonBatchSize,
        fillWithGroupMode,
        customTitle,
        guideMode,
        preRollFiller,
        midRollFiller,
        postRollFiller,
        tailFiller,
        fallbackFiller,
        watermarks,
        graphicsElements,
        preferredAudioLanguageCode,
        preferredAudioTitle,
        preferredSubtitleLanguageCode,
        subtitleMode)
    {
    }
}
