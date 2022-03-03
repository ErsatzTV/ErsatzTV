using System;
using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
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
        FillerPresetViewModel midRollFiller,
        FillerPresetViewModel postRollFiller,
        FillerPresetViewModel tailFiller,
        FillerPresetViewModel fallbackFiller) : base(
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
        midRollFiller,
        postRollFiller,
        tailFiller,
        fallbackFiller) =>
        Count = count;

    public int Count { get; }
}