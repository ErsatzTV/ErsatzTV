namespace ErsatzTV.Core.Domain;

public abstract class MediaServerItemEtag
{
    public abstract string MediaServerItemId { get; }
    public abstract string Etag { get; set; }
    public abstract MediaItemState State { get; set; }
}
