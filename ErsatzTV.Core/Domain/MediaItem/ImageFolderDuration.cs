namespace ErsatzTV.Core.Domain;

public class ImageFolderDuration
{
    public int Id { get; set; }
    public int LibraryFolderId { get; set; }
    public LibraryFolder LibraryFolder { get; set; }
    public int DurationSeconds { get; set; }
}
