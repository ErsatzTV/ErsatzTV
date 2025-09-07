using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlWaitUntil
{
    [Description("The time of day to wait (insert unscheduled time) until")]
    public string When { get; set; }

    [Description("When true, will wait until the specified time tomorrow if it has already passed today.")]
    public bool Tomorrow { get; set; }

    [Description("When true, the current time of the playout build is allowed to move backward when the playout is reset.")]
    public bool RewindOnReset { get; set; }
}
