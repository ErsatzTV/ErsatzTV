using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Images
{
    public interface IImageCache
    {
        Task<Either<BaseError, byte[]>> ResizeImage(byte[] imageBuffer, int height);
        Task<Either<BaseError, string>> SaveArtworkToCache(byte[] imageBuffer, ArtworkKind artworkKind);
        string CopyArtworkToCache(string path, ArtworkKind artworkKind);
    }
}
