using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlStartEpgGroup
{
    [Description("When true, will make a new EPG group. When false, will continue the existing EPG group.")]
    public bool Advance { get; set; } = true;

    [Description("Custom title to apply to all items in the EPG group.")]
    public string CustomTitle { get; set; }
}
