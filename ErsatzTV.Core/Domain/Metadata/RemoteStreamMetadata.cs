namespace ErsatzTV.Core.Domain;

public class RemoteStreamMetadata : Metadata
{
    public string ContentRating { get; set; }
    public string Plot { get; set; }
    public int RemoteStreamId { get; set; }
    public RemoteStream RemoteStream { get; set; }
}
