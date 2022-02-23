using System;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;

namespace ErsatzTV.Core.Scheduling
{
    public record PlayoutBuilderState(
        IScheduleItemsEnumerator ScheduleItemsEnumerator,
        Option<int> MultipleRemaining,
        Option<DateTimeOffset> DurationFinish,
        bool InFlood,
        bool InDurationFiller,
        int NextGuideGroup,
        DateTimeOffset CurrentTime)
    {
        public int IncrementGuideGroup => (NextGuideGroup + 1) % 10000;
        public int DecrementGuideGroup => (NextGuideGroup - 1) % 10000;
    }
}
