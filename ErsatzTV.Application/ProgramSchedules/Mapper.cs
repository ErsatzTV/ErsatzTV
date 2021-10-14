using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    internal static class Mapper
    {
        internal static ProgramScheduleViewModel ProjectToViewModel(ProgramSchedule programSchedule) =>
            new(
                programSchedule.Id,
                programSchedule.Name,
                programSchedule.KeepMultiPartEpisodesTogether,
                programSchedule.TreatCollectionsAsShows);

        internal static ProgramScheduleItemViewModel ProjectToViewModel(ProgramScheduleItem programScheduleItem) =>
            programScheduleItem switch
            {
                ProgramScheduleItemDuration duration =>
                    new ProgramScheduleItemDurationViewModel(
                        duration.Id,
                        duration.Index,
                        duration.StartType,
                        duration.StartTime,
                        duration.CollectionType,
                        duration.Collection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.Collection)
                            : null,
                        duration.MultiCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.MultiCollection)
                            : null,
                        duration.SmartCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.SmartCollection)
                            : null,
                        duration.MediaItem switch
                        {
                            Show show => MediaItems.Mapper.ProjectToViewModel(show),
                            Season season => MediaItems.Mapper.ProjectToViewModel(season),
                            Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                            _ => null
                        },
                        duration.PlaybackOrder,
                        duration.PlayoutDuration,
                        duration.TailMode,
                        duration.TailCollectionType,
                        duration.TailCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.TailCollection)
                            : null,
                        duration.TailMultiCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.TailMultiCollection)
                            : null,
                        duration.TailSmartCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.TailSmartCollection)
                            : null,
                        duration.TailMediaItem switch
                        {
                            Show show => MediaItems.Mapper.ProjectToViewModel(show),
                            Season season => MediaItems.Mapper.ProjectToViewModel(season),
                            Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                            _ => null
                        },
                        duration.CustomTitle),
                ProgramScheduleItemFlood flood =>
                    new ProgramScheduleItemFloodViewModel(
                        flood.Id,
                        flood.Index,
                        flood.StartType,
                        flood.StartTime,
                        flood.CollectionType,
                        flood.Collection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(flood.Collection)
                            : null,
                        flood.MultiCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(flood.MultiCollection)
                            : null,
                        flood.SmartCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(flood.SmartCollection)
                            : null,
                        flood.MediaItem switch
                        {
                            Show show => MediaItems.Mapper.ProjectToViewModel(show),
                            Season season => MediaItems.Mapper.ProjectToViewModel(season),
                            Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                            _ => null
                        },
                        flood.PlaybackOrder,
                        flood.CustomTitle),
                ProgramScheduleItemMultiple multiple =>
                    new ProgramScheduleItemMultipleViewModel(
                        multiple.Id,
                        multiple.Index,
                        multiple.StartType,
                        multiple.StartTime,
                        multiple.CollectionType,
                        multiple.Collection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(multiple.Collection)
                            : null,
                        multiple.MultiCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(multiple.MultiCollection)
                            : null,
                        multiple.SmartCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(multiple.SmartCollection)
                            : null,
                        multiple.MediaItem switch
                        {
                            Show show => MediaItems.Mapper.ProjectToViewModel(show),
                            Season season => MediaItems.Mapper.ProjectToViewModel(season),
                            Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                            _ => null
                        },
                        multiple.PlaybackOrder,
                        multiple.Count,
                        multiple.CustomTitle),
                ProgramScheduleItemOne one =>
                    new ProgramScheduleItemOneViewModel(
                        one.Id,
                        one.Index,
                        one.StartType,
                        one.StartTime,
                        one.CollectionType,
                        one.Collection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(one.Collection)
                            : null,
                        one.MultiCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(one.MultiCollection)
                            : null,
                        one.SmartCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(one.SmartCollection)
                            : null,
                        one.MediaItem switch
                        {
                            Show show => MediaItems.Mapper.ProjectToViewModel(show),
                            Season season => MediaItems.Mapper.ProjectToViewModel(season),
                            Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                            _ => null
                        },
                        one.PlaybackOrder,
                        one.CustomTitle),
                _ => throw new NotSupportedException(
                    $"Unsupported program schedule item type {programScheduleItem.GetType().Name}")
            };
    }
}
