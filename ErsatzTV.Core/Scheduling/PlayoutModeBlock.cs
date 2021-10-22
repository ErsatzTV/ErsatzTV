using System;

namespace ErsatzTV.Core.Scheduling
{
    public record PlayoutModeBlock(DateTimeOffset StartTime, DateTimeOffset FinishTime);
}
