using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Jellyfin
{
    public class JellyfinMovieLibraryScanner : IJellyfinMovieLibraryScanner
    {
        public async Task<Either<BaseError, Unit>> ScanLibrary(string address, string apiKey, JellyfinLibrary library)
        {
            return Unit.Default;
        }
    }
}
