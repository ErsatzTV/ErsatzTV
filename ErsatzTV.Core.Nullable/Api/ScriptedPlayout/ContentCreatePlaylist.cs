using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ContentCreatePlaylist
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public required string Key { get; set; }

    [Description("List of playlist items")]
    public required List<PlaylistItem> Items { get; set; }
}
