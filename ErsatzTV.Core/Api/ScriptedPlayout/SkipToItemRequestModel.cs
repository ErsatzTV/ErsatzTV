using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record SkipToItemRequestModel
{
    [Description("The 'key' for the content")]
    public string Content { get; set; }

    [Description("The season number")]
    public int Season { get; set; }

    [Description("The episode number")]
    public int Episode { get; set; }
}
