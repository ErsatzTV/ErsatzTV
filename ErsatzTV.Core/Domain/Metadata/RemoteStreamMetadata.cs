namespace ErsatzTV.Core.Domain;

public class RemoteStreamMetadata : Metadata
{
    public int RemoteStreamId { get; set; }
    public RemoteStream RemoteStream { get; set; }
}
