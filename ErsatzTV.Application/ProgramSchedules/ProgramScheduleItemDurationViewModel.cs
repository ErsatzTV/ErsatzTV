﻿using ErsatzTV.Application.Filler;
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
        FillWithGroupMode fillWithGroupMode,
        TimeSpan playoutDuration,
        TailMode tailMode,
        int discardToFillAttempts,
        string customTitle,
        GuideMode guideMode,
        FillerPresetViewModel preRollFiller,
        FillerPresetViewModel midRollFiller,
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
        PlayoutMode.Duration,
        collectionType,
        collection,
        multiCollection,
        smartCollection,
        mediaItem,
        playbackOrder,
        fillWithGroupMode,
        customTitle,
        guideMode,
        preRollFiller,
        midRollFiller,
        postRollFiller,
        tailFiller,
        fallbackFiller,
        watermark,
        preferredAudioLanguageCode,
        preferredAudioTitle,
        preferredSubtitleLanguageCode,
        subtitleMode)
    {
        PlayoutDuration = playoutDuration;
        TailMode = tailMode;
        DiscardToFillAttempts = discardToFillAttempts;
    }

    public TimeSpan PlayoutDuration { get; }
    public TailMode TailMode { get; }
    public int DiscardToFillAttempts { get; }
}
