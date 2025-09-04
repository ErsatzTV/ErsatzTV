using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddPlaylistRequestModel
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public string Key { get; set; }

    [Description("The name of the existing playlist")]
    public string Playlist { get; set; }

    [Description("The name of the existing playlist group that contains the named playlist")]
    public string PlaylistGroup { get; set; }
}
