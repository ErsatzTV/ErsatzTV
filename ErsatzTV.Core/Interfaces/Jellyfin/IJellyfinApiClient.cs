using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinApiClient
{
    Task<Either<BaseError, JellyfinServerInformation>> GetServerInformation(string address, string apiKey);
    Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey);
    Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey);

    IAsyncEnumerable<JellyfinMovie> GetMovieLibraryItems(string address, string apiKey, JellyfinLibrary library);

    IAsyncEnumerable<JellyfinShow> GetShowLibraryItems(string address, string apiKey, JellyfinLibrary library);

    IAsyncEnumerable<JellyfinSeason> GetSeasonLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string showId);

    IAsyncEnumerable<JellyfinEpisode> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string seasonId);

    Task<Either<BaseError, List<JellyfinCollection>>> GetCollectionLibraryItems(
        string address,
        string apiKey,
        int mediaSourceId);

    Task<Either<BaseError, List<MediaItem>>> GetCollectionItems(
        string address,
        string apiKey,
        int mediaSourceId,
        string collectionId);

    Task<Either<BaseError, int>> GetLibraryItemCount(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string parentId,
        string includeItemTypes,
        bool excludeFolders);
}
