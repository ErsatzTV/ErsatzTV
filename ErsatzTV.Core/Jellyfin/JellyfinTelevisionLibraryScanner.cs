using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Jellyfin
{
    public class JellyfinTelevisionLibraryScanner : IJellyfinTelevisionLibraryScanner
    {
        public async Task<Either<BaseError, Unit>> ScanLibrary(string address, string apiKey, JellyfinLibrary library)
        {
            return Unit.Default;
        }
    }
}
