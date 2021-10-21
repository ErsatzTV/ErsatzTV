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
        int NextGuideGroup,
        DateTimeOffset CurrentTime)
    {
        public int IncrementGuideGroup => (NextGuideGroup + 1) % 10000;
    }
}
