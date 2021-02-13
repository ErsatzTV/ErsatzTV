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
                        MediaCollections.Mapper.ProjectToViewModel(duration.MediaCollection),
                        duration.PlayoutDuration,
                        duration.OfflineTail),
                ProgramScheduleItemFlood flood =>
                    new ProgramScheduleItemFloodViewModel(
                        flood.Id,
                        flood.Index,
                        flood.StartType,
                        flood.StartTime,
                        MediaCollections.Mapper.ProjectToViewModel(flood.MediaCollection)),
                ProgramScheduleItemMultiple multiple =>
                    new ProgramScheduleItemMultipleViewModel(
                        multiple.Id,
                        multiple.Index,
                        multiple.StartType,
                        multiple.StartTime,
                        MediaCollections.Mapper.ProjectToViewModel(multiple.MediaCollection),
                        multiple.Count),
                ProgramScheduleItemOne one =>
                    new ProgramScheduleItemOneViewModel(
                        one.Id,
                        one.Index,
                        one.StartType,
                        one.StartTime,
                        MediaCollections.Mapper.ProjectToViewModel(one.MediaCollection)),
                _ => throw new NotSupportedException(
                    $"Unsupported program schedule item type {programScheduleItem.GetType().Name}")
            };
    }
}
