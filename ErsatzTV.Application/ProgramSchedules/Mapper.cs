using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    internal static class Mapper
    {
        internal static ProgramScheduleViewModel ProjectToViewModel(ProgramSchedule programSchedule) =>
            new(programSchedule.Id, programSchedule.Name, programSchedule.MediaCollectionPlaybackOrder);

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
                        duration.MediaCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(duration.MediaCollection)
                            : null,
                        duration.TelevisionShow != null
                            ? Television.Mapper.ProjectToViewModel(duration.TelevisionShow)
                            : null,
                        duration.TelevisionSeason != null
                            ? Television.Mapper.ProjectToViewModel(duration.TelevisionSeason)
                            : null,
                        duration.PlayoutDuration,
                        duration.OfflineTail),
                ProgramScheduleItemFlood flood =>
                    new ProgramScheduleItemFloodViewModel(
                        flood.Id,
                        flood.Index,
                        flood.StartType,
                        flood.StartTime,
                        flood.CollectionType,
                        flood.MediaCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(flood.MediaCollection)
                            : null,
                        flood.TelevisionShow != null
                            ? Television.Mapper.ProjectToViewModel(flood.TelevisionShow)
                            : null,
                        flood.TelevisionSeason != null
                            ? Television.Mapper.ProjectToViewModel(flood.TelevisionSeason)
                            : null),
                ProgramScheduleItemMultiple multiple =>
                    new ProgramScheduleItemMultipleViewModel(
                        multiple.Id,
                        multiple.Index,
                        multiple.StartType,
                        multiple.StartTime,
                        multiple.CollectionType,
                        multiple.MediaCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(multiple.MediaCollection)
                            : null,
                        multiple.TelevisionShow != null
                            ? Television.Mapper.ProjectToViewModel(multiple.TelevisionShow)
                            : null,
                        multiple.TelevisionSeason != null
                            ? Television.Mapper.ProjectToViewModel(multiple.TelevisionSeason)
                            : null,
                        multiple.Count),
                ProgramScheduleItemOne one =>
                    new ProgramScheduleItemOneViewModel(
                        one.Id,
                        one.Index,
                        one.StartType,
                        one.StartTime,
                        one.CollectionType,
                        one.MediaCollection != null
                            ? MediaCollections.Mapper.ProjectToViewModel(one.MediaCollection)
                            : null,
                        one.TelevisionShow != null ? Television.Mapper.ProjectToViewModel(one.TelevisionShow) : null,
                        one.TelevisionSeason != null
                            ? Television.Mapper.ProjectToViewModel(one.TelevisionSeason)
                            : null),
                _ => throw new NotSupportedException(
                    $"Unsupported program schedule item type {programScheduleItem.GetType().Name}")
            };
    }
}
