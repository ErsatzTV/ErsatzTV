using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Plex;

public class PlexItemEtag : MediaServerItemEtag
{
    public string Key { get; set; }
    public override string MediaServerItemId => Key;
    public override string Etag { get; set; }
    public override MediaItemState State { get; set; }
}
