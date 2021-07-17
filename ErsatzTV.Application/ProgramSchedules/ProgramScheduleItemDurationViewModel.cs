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
            NamedMediaItemViewModel mediaItem,
            PlaybackOrder playbackOrder,
            TimeSpan playoutDuration,
            bool offlineTail,
            string customTitle) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.Duration,
            collectionType,
            collection,
            multiCollection,
            mediaItem,
            playbackOrder,
            customTitle)
        {
            PlayoutDuration = playoutDuration;
            OfflineTail = offlineTail;
        }

        public TimeSpan PlayoutDuration { get; }
        public bool OfflineTail { get; }
    }
}
