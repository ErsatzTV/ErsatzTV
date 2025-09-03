namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddPlaylistRequestModel
{
    public string Key { get; set; }
    public string Playlist { get; set; }
    public string PlaylistGroup { get; set; }
}
