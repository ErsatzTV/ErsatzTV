using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling.ClassicScheduling;

public class GetStartTimeAfterTests
{
    [Test]
    [Ignore("This test isn't ready to run yet")]
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

        DateTimeOffset result =
            PlayoutModeSchedulerBase<ProgramScheduleItem>.GetStartTimeAfter(state, scheduleItem, Option<ILogger>.None);

        result.ShouldBe(DateTimeOffset.Parse("2025-11-02T02:00:00-06:00"));
    }

    [Test]
    public void Should_Return_Current_Time_With_Flexible_Fixed_Start()
    {
        // 12:05 am
        var scheduleItem = new ProgramScheduleItemOne
        {
            StartTime = TimeSpan.FromMinutes(5),
            FixedStartTimeBehavior = null,
            ProgramSchedule = new ProgramSchedule
            {
                FixedStartTimeBehavior = FixedStartTimeBehavior.Flexible
            }
        };

        var state = new PlayoutBuilderState(
            0,
            null,
            Option<int>.None,
            Option<DateTimeOffset>.None,
            false,
            false,
            0,
            DateTimeOffset.Parse("2025-08-29T00:10:00-05:00"));

        DateTimeOffset result =
            PlayoutModeSchedulerBase<ProgramScheduleItem>.GetStartTimeAfter(state, scheduleItem, Option<ILogger>.None);

        result.ShouldBe(DateTimeOffset.Parse("2025-08-29T00:10:00-05:00"));
    }
}
