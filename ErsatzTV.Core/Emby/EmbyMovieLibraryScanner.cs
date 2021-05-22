using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using LanguageExt;

namespace ErsatzTV.Core.Emby
{
    public class EmbyMovieLibraryScanner : IEmbyMovieLibraryScanner
    {
        public async Task<Either<BaseError, Unit>> ScanLibrary(
            string address,
            string apiKey,
            EmbyLibrary library,
            string ffprobePath) => Unit.Default;
    }
}
