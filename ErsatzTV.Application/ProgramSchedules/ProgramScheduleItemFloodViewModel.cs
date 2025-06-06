﻿using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public record ProgramScheduleItemFloodViewModel : ProgramScheduleItemViewModel
{
    public ProgramScheduleItemFloodViewModel(
        int id,
        int index,
        StartType startType,
        TimeSpan? startTime,
        FixedStartTimeBehavior? fixedStartTimeBehavior,
        ProgramScheduleItemCollectionType collectionType,
        MediaCollectionViewModel collection,
        MultiCollectionViewModel multiCollection,
        SmartCollectionViewModel smartCollection,
        PlaylistViewModel playlist,
        NamedMediaItemViewModel mediaItem,
        PlaybackOrder playbackOrder,
        FillWithGroupMode fillWithGroupMode,
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
        fixedStartTimeBehavior,
        PlayoutMode.Flood,
        collectionType,
        collection,
        multiCollection,
        smartCollection,
        playlist,
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
    }
}
