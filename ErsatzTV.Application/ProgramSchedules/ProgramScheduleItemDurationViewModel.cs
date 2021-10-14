using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
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
            ProgramScheduleItemCollectionType tailCollectionType,
            MediaCollectionViewModel tailCollection,
            MultiCollectionViewModel tailMultiCollection,
            SmartCollectionViewModel tailSmartCollection,
            NamedMediaItemViewModel tailMediaItem,
            string customTitle,
            GuideMode guideMode) : base(
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
            guideMode)
        {
            PlayoutDuration = playoutDuration;
            TailMode = tailMode;
            TailCollectionType = tailCollectionType;
            TailCollection = tailCollection;
            TailMultiCollection = tailMultiCollection;
            TailSmartCollection = tailSmartCollection;
            TailMediaItem = tailMediaItem;
        }

        public TimeSpan PlayoutDuration { get; }
        public TailMode TailMode { get; }
        public ProgramScheduleItemCollectionType TailCollectionType { get; }

        public MediaCollectionViewModel TailCollection { get; }
        public MultiCollectionViewModel TailMultiCollection { get; }
        public SmartCollectionViewModel TailSmartCollection { get; }
        public NamedMediaItemViewModel TailMediaItem { get; }

    }
}
