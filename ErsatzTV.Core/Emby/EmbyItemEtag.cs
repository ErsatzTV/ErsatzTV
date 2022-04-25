using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Emby;

public class EmbyItemEtag
{
    public string ItemId { get; set; }
    public string Etag { get; set; }
    public MediaItemState State { get; set; }
}
