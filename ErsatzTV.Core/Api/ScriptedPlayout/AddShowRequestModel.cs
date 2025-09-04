using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddShowRequestModel
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public string Key { get; set; }

    [Description("List of show identifiers")]
    public Dictionary<string, string> Guids { get; set; } = [];

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; set; } = "shuffle";
}
