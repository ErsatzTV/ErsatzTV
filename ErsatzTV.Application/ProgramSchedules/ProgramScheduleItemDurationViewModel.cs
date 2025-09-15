﻿using ErsatzTV.Application.Filler;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public record ProgramScheduleItemDurationViewModel : ProgramScheduleItemViewModel
{
    public ProgramScheduleItemDurationViewModel(
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
        PlaybackOrder playbackOrder,
        MarathonGroupBy marathonGroupBy,
        bool marathonShuffleGroups,
        bool marathonShuffleItems,
        int? marathonBatchSize,
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
        PlayoutMode.Duration,
        collectionType,
        collection,
        multiCollection,
        smartCollection,
        rerunCollection,
        playlist,
        mediaItem,
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
        PlayoutDuration = playoutDuration;
        TailMode = tailMode;
        DiscardToFillAttempts = discardToFillAttempts;
    }

    public TimeSpan PlayoutDuration { get; }
    public TailMode TailMode { get; }
    public int DiscardToFillAttempts { get; }
}
