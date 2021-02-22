using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Television;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public record ProgramScheduleItemOneViewModel : ProgramScheduleItemViewModel
    {
        public ProgramScheduleItemOneViewModel(
            int id,
            int index,
            StartType startType,
            TimeSpan? startTime,
            ProgramScheduleItemCollectionType collectionType,
            MediaCollectionViewModel mediaCollection,
            TelevisionShowViewModel televisionShow,
            TelevisionSeasonViewModel televisionSeason) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.One,
            collectionType,
            mediaCollection,
            televisionShow,
            televisionSeason)
        {
        }
    }
}
