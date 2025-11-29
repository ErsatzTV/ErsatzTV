using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ContentPlaylist
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public required string Key { get; set; }

    [Description("The name of the existing playlist")]
    public required string Playlist { get; set; }

    [Description("The name of the existing playlist group that contains the named playlist")]
    public required string PlaylistGroup { get; set; }
}
