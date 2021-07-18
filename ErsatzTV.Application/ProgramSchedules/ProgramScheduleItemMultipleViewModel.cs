using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
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
            NamedMediaItemViewModel mediaItem,
            PlaybackOrder playbackOrder,
            int count,
            string customTitle) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.Multiple,
            collectionType,
            collection,
            multiCollection,
            mediaItem,
            playbackOrder,
            customTitle) =>
            Count = count;

        public int Count { get; }
    }
}
