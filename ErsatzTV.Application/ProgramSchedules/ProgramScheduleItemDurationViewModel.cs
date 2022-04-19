using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules;

public record ProgramScheduleItemDurationViewModel : ProgramScheduleItemViewModel
{
    public ProgramScheduleItemDurationViewModel(
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
        TimeSpan playoutDuration,
        TailMode tailMode,
        string customTitle,
        GuideMode guideMode,
        FillerPresetViewModel preRollFiller,
        FillerPresetViewModel midRollFiller,
        FillerPresetViewModel postRollFiller,
        FillerPresetViewModel tailFiller,
        FillerPresetViewModel fallbackFiller,
        WatermarkViewModel watermark) : base(
        id,
        index,
        startType,
        startTime,
        PlayoutMode.Duration,
        collectionType,
        collection,
        multiCollection,
        smartCollection,
        mediaItem,
        playbackOrder,
        customTitle,
        guideMode,
        preRollFiller,
        midRollFiller,
        postRollFiller,
        tailFiller,
        fallbackFiller,
        watermark)
    {
        PlayoutDuration = playoutDuration;
        TailMode = tailMode;
    }

    public TimeSpan PlayoutDuration { get; }
    public TailMode TailMode { get; }
}
