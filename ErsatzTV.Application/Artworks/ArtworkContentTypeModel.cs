using System.Net;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Artworks;

public record ArtworkContentTypeModel(string Path, string ContentType)
{
    public static readonly ArtworkContentTypeModel None = new(string.Empty, string.Empty);

    public bool IsExternalUrl => Artwork.IsExternalUrl(Path);

    public bool HasContentType => !string.IsNullOrWhiteSpace(ContentType);

    public string UrlWithContentType => string.IsNullOrWhiteSpace(ContentType)
        ? Path
        : $"{Path}?contentType={WebUtility.UrlEncode(ContentType)}";
}
