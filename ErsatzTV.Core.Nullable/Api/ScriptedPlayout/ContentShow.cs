using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ContentShow
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public required string Key { get; set; }

    [Description("List of show identifiers")]
    public required Dictionary<string, string> Guids { get; set; } = [];

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; set; } = "shuffle";
}
