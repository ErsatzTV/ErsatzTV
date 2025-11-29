using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ContentSearch
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public required string Key { get; set; }

    [Description("The search query")]
    public required string Query { get; set; }

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; set; } = "shuffle";
}
