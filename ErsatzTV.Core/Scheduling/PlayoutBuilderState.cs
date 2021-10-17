using System;
using LanguageExt;

namespace ErsatzTV.Core.Scheduling
{
    public record PlayoutBuilderState(
        int ScheduleItemIndex,
        Option<int> MultipleRemaining,
        Option<DateTimeOffset> DurationFinish,
        bool InFlood,
        bool InDurationFiller,
        bool CustomGroup,
        DateTimeOffset CurrentTime);
}
