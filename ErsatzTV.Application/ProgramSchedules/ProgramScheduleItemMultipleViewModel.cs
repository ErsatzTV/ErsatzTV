using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules;

public record ProgramScheduleItemMultipleViewModel : ProgramScheduleItemViewModel
{
    public ProgramScheduleItemMultipleViewModel(
        int id,
        int index,
        StartType startType,
        TimeSpan? startTime,
        ProgramScheduleItemCollectionType collectionType,
        MediaCollectionViewModel collection,
        MultiCollectionViewModel multiCollection,
        SmartCollectionViewModel smartCollection,
        NamedMediaItemViewModel mediaItem,
        PlaybackOrder playbackOrder,
        int count,
        string customTitle,
        GuideMode guideMode,
        FillerPresetViewModel preRollFiller,
        FillerPresetViewModel midRollEnterFiller,
        FillerPresetViewModel midRollFiller,
        FillerPresetViewModel midRollExitFiller,
        FillerPresetViewModel postRollFiller,
        FillerPresetViewModel tailFiller,
        FillerPresetViewModel fallbackFiller,
        WatermarkViewModel watermark,
        string preferredAudioLanguageCode,
        string preferredAudioTitle,
        string preferredSubtitleLanguageCode,
        ChannelSubtitleMode? subtitleMode) : base(
        id,
        index,
        startType,
        startTime,
        PlayoutMode.Multiple,
        collectionType,
        collection,
        multiCollection,
        smartCollection,
        mediaItem,
        playbackOrder,
        customTitle,
        guideMode,
        preRollFiller,
        midRollEnterFiller,
        midRollFiller,
        midRollExitFiller,
        postRollFiller,
        tailFiller,
        fallbackFiller,
        watermark,
        preferredAudioLanguageCode,
        preferredAudioTitle,
        preferredSubtitleLanguageCode,
        subtitleMode) =>
        Count = count;

    public int Count { get; }
}
