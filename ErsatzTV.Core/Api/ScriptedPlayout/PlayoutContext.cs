using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record PlayoutContext
{
    [Description("The current time of the playout build")]
    public DateTimeOffset CurrentTime { get; set; }

    [Description("The start time of the playout build")]
    public DateTimeOffset StartTime { get; set; }

    [Description("The finish time of the playout build")]
    public DateTimeOffset FinishTime { get; set; }

    [Description("Indicates whether the current playout build is complete")]
    public bool IsDone { get; set; }
}
