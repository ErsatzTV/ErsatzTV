namespace ErsatzTV.Core.Domain;

public class LibraryFolder
{
    public int Id { get; set; }
    public string Path { get; set; }
    public int LibraryPathId { get; set; }
    public LibraryPath LibraryPath { get; set; }
    public string Etag { get; set; }
}
