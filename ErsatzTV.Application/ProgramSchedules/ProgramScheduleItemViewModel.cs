using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Television;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public abstract record ProgramScheduleItemViewModel(
        int Id,
        int Index,
        StartType StartType,
        TimeSpan? StartTime,
        PlayoutMode PlayoutMode,
        ProgramScheduleItemCollectionType CollectionType,
        MediaCollectionViewModel Collection,
        TelevisionShowViewModel TelevisionShow,
        TelevisionSeasonViewModel TelevisionSeason)
    {
        public string Name => CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => Collection?.Name,
            ProgramScheduleItemCollectionType.TelevisionShow => $"{TelevisionShow?.Title} ({TelevisionShow?.Year})",
            ProgramScheduleItemCollectionType.TelevisionSeason =>
                $"{TelevisionSeason?.Title} ({TelevisionSeason?.Plot})",
            _ => string.Empty
        };
    }
}
