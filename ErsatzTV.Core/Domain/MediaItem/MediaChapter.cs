namespace ErsatzTV.Core.Domain;

public class MediaChapter
{
    public int Id { get; set; }
    public int MediaVersionId { get; set; }
    public MediaVersion MediaVersion { get; set; }
    public long ChapterId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Title { get; set; }
}
