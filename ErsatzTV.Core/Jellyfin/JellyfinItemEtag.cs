using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Jellyfin;

public class JellyfinItemEtag : MediaServerItemEtag
{
    public string ItemId { get; set; }
    public override string MediaServerItemId => ItemId;
    public override string Etag { get; set; }
    public override MediaItemState State { get; set; }
}
