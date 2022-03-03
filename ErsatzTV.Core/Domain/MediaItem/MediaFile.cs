namespace ErsatzTV.Core.Domain;

public class MediaFile
{
    public int Id { get; set; }
    public string Path { get; set; }

    public int MediaVersionId { get; set; }
    public MediaVersion MediaVersion { get; set; }
}