namespace ErsatzTV.Core.Domain;

public class ChapterMediaItem : MediaItem
{
    public ChapterMediaItem(int id, MediaItem mediaItem, ChapterMediaVersion chapterMediaVersion)
    {
        Id = id;
        MediaItemId = mediaItem.Id;
        MediaVersion = chapterMediaVersion;
    }

    public int MediaItemId { get; }
    public ChapterMediaVersion MediaVersion { get; }
}
