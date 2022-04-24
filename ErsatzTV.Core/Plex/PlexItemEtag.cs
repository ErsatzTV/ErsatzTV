using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Plex;

public class PlexItemEtag
{
    public string Key { get; set; }
    public string Etag { get; set; }
    public MediaItemState State { get; set; }
}
