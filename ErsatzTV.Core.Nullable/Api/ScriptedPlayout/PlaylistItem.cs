using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record PlaylistItem
{
    [Description("The 'key' for the content")]
    public required string Content { get; set; }

    public required int Count { get; set; }
}
