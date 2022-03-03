namespace ErsatzTV.Core.Domain;

public class OtherVideoMetadata : Metadata
{
    public int OtherVideoId { get; set; }
    public OtherVideo OtherVideo { get; set; }
}