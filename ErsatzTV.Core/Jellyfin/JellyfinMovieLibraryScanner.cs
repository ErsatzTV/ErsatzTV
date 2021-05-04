using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Jellyfin
{
    public class JellyfinMovieLibraryScanner : IJellyfinMovieLibraryScanner
    {
        private readonly IJellyfinApiClient _jellyfinApiClient;

        public JellyfinMovieLibraryScanner(IJellyfinApiClient jellyfinApiClient) =>
            _jellyfinApiClient = jellyfinApiClient;

        public async Task<Either<BaseError, Unit>> ScanLibrary(string address, string apiKey, JellyfinLibrary library)
        {
            await _jellyfinApiClient.GetLibraryItems(address, apiKey, library.MediaSourceId, library.ItemId);
            return Unit.Default;
        }
    }
}
