using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record PlaylistItem
{
    [Description("The 'key' for the content")]
    public string Content { get; set; }

    public int Count { get; set; }
}
