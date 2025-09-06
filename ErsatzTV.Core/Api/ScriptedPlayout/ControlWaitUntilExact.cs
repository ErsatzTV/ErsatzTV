using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlWaitUntilExact
{
    [Description("The time to wait (insert unscheduled time) until")]
    public DateTimeOffset When { get; set; }

    [Description("When true, the current time of the playout build is allowed to move backward when the playout is reset.")]
    public bool RewindOnReset { get; set; }
}
