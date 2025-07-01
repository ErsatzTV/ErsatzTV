using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutContentPlaylistItem : YamlPlayoutContentItem
{
    public string Playlist { get; set; }

    [YamlMember(Alias = "playlist_group", ApplyNamingConventions = false)]
    public string PlaylistGroup { get; set; }

    public override string Order
    {
        get => "none";
        set => throw new NotSupportedException();
    }
}
