using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlSkipToItem
{
    [Description("The 'key' for the content")]
    public required string Content { get; set; }

    [Description("The season number")]
    public required int Season { get; set; }

    [Description("The episode number")]
    public required int Episode { get; set; }
}
