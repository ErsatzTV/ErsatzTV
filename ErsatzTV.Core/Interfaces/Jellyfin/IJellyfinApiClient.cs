using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinApiClient
{
    Task<Either<BaseError, JellyfinServerInformation>> GetServerInformation(string address, string apiKey);
    Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey);

    IAsyncEnumerable<Tuple<JellyfinMovie, int>> GetMovieLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library);

    IAsyncEnumerable<Tuple<JellyfinShow, int>> GetShowLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library);

    IAsyncEnumerable<Tuple<JellyfinSeason, int>> GetSeasonLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string showId);

    IAsyncEnumerable<Tuple<JellyfinEpisode, int>> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string seasonId);

    IAsyncEnumerable<Tuple<JellyfinCollection, int>> GetCollectionLibraryItems(
        string address,
        string apiKey,
        int mediaSourceId);

    IAsyncEnumerable<Tuple<MediaItem, int>> GetCollectionItems(
        string address,
        string apiKey,
        int mediaSourceId,
        string collectionId);

    Task<Either<BaseError, MediaVersion>> GetPlaybackInfo(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string itemId);
}
