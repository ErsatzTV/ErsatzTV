using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using Shouldly;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling.ClassicScheduling;

public class GetStartTimeAfterTests
{
    [Test]
    public void Should_Return_Correct_Time_On_Dst_Fall_Back()
    {
        var scheduleItem = new ProgramScheduleItemOne
        {
            StartTime = TimeSpan.FromHours(3)
        };

        var state = new PlayoutBuilderState(
            0,
            null,
            Option<int>.None,
            Option<DateTimeOffset>.None,
            false,
            false,
            0,
            DateTimeOffset.Parse("2025-11-02T00:00:00-05:00"));

        DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>.GetStartTimeAfter(state, scheduleItem);

        result.ShouldBe(DateTimeOffset.Parse("2025-11-02T02:00:00-06:00"));
    }
}