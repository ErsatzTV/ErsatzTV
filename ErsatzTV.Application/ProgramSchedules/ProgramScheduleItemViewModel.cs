using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public abstract record ProgramScheduleItemViewModel(
        int Id,
        int Index,
        StartType StartType,
        TimeSpan? StartTime,
        PlayoutMode PlayoutMode,
        MediaCollectionViewModel MediaCollection);
}
