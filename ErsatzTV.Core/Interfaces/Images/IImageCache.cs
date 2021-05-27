using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Images
{
    public interface IImageCache
    {
        Task<Either<BaseError, byte[]>> ResizeImage(byte[] imageBuffer, int height);
        Task<Either<BaseError, string>> SaveArtworkToCache(byte[] imageBuffer, ArtworkKind artworkKind);
        Task<Either<BaseError, string>> CopyArtworkToCache(string path, ArtworkKind artworkKind);
        string GetPathForImage(string fileName, ArtworkKind artworkKind, Option<int> maybeMaxHeight);
    }
}
