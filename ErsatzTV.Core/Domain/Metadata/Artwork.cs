namespace ErsatzTV.Core.Domain;

public class Artwork
{
    public int Id { get; set; }
    public string Path { get; set; }
    public string SourcePath { get; set; }
    public string BlurHash43 { get; set; }
    public string BlurHash54 { get; set; }
    public string BlurHash64 { get; set; }
    public string OriginalContentType { get; set; }
    public ArtworkKind ArtworkKind { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime DateUpdated { get; set; }

    public bool IsExternalUrl() => IsExternalUrl(Path);

    public static bool IsExternalUrl(string path) =>
        Uri.TryCreate(path ?? string.Empty, UriKind.Absolute, out Uri uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
}
