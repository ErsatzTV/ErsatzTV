using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Emby
{
    public interface IEmbyTelevisionLibraryScanner
    {
        Task<Either<BaseError, Unit>> ScanLibrary(
            string address,
            string apiKey,
            EmbyLibrary library,
            string ffprobePath);
    }
}
