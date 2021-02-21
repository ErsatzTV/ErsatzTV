using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Television;
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
            MediaCollectionViewModel mediaCollection,
            TelevisionShowViewModel televisionShow,
            TelevisionSeasonViewModel televisionSeason,
            int count) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.Multiple,
            collectionType,
            mediaCollection,
            televisionShow,
            televisionSeason) =>
            Count = count;

        public int Count { get; }
    }
}
