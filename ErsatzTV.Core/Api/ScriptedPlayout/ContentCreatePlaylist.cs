using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ContentCreatePlaylist
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public string Key { get; set; }

    [Description("List of playlist items")]
    public List<PlaylistItem> Items { get; set; }
}
