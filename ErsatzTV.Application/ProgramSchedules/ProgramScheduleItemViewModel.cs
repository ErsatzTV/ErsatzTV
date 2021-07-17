using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
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
        MultiCollectionViewModel MultiCollection,
        NamedMediaItemViewModel MediaItem,
        string CustomTitle)
    {
        public string Name => CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => Collection?.Name,
            ProgramScheduleItemCollectionType.TelevisionShow =>
                MediaItem?.Name, // $"{TelevisionShow?.Title} ({TelevisionShow?.Year})",
            ProgramScheduleItemCollectionType.TelevisionSeason =>
                MediaItem?.Name, // $"{TelevisionSeason?.Title} ({TelevisionSeason?.Plot})",
            ProgramScheduleItemCollectionType.Artist =>
                MediaItem?.Name,
            ProgramScheduleItemCollectionType.MultiCollection =>
                MultiCollection?.Name,
            _ => string.Empty
        };
    }
}
