using System.Threading.Tasks;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Images
{
    public interface IImageCache
    {
        Task<Either<BaseError, byte[]>> ResizeImage(byte[] imageBuffer, int height);
        Task<Either<BaseError, string>> ResizeAndSaveImage(byte[] imageBuffer, int? height, int? width);
        Task<Either<BaseError, string>> SaveImage(byte[] imageBuffer);
    }
}
