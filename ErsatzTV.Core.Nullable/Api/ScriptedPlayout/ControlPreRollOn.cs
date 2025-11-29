using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlPreRollOn
{
    [Description("The 'key' for the scripted playlist")]
    public required string Playlist { get; set; }
}
