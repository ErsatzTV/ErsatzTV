using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.Interfaces.Images;

public interface IImageCache
{
    Task<Either<BaseError, string>> SaveArtworkToCache(Stream stream, ArtworkKind artworkKind);
    Task<Either<BaseError, string>> CopyArtworkToCache(string path, ArtworkKind artworkKind);
    string GetPathForImage(string fileName, ArtworkKind artworkKind, Option<int> maybeMaxHeight);
    string CalculateBlurHash(string fileName, ArtworkKind artworkKind, int x, int y);
    string WriteBlurHash(string blurHash, IDisplaySize targetSize);
}