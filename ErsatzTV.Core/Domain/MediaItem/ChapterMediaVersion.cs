namespace ErsatzTV.Core.Domain;

public class ChapterMediaVersion : MediaVersion
{
    public ChapterMediaVersion(MediaChapter chapter)
    {
        InPoint = chapter.StartTime;
        Duration = chapter.EndTime - chapter.StartTime;
        Title = chapter.Title;
    }

    public TimeSpan InPoint { get; }
    public string Title { get; }
}
