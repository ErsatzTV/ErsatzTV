using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinApiClient
{
    Task<Either<BaseError, JellyfinServerInformation>> GetServerInformation(string address, string authorizationHeader);
    Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string authorizationHeader);

    IAsyncEnumerable<Tuple<JellyfinMovie, int>> GetMovieLibraryItems(
        string address,
        string authorizationHeader,
        JellyfinLibrary library);

    IAsyncEnumerable<Tuple<JellyfinShow, int>> GetShowLibraryItemsWithoutPeople(
        string address,
        string authorizationHeader,
        JellyfinLibrary library);

    IAsyncEnumerable<Tuple<JellyfinSeason, int>> GetSeasonLibraryItems(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string showId);

    IAsyncEnumerable<Tuple<JellyfinEpisode, int>> GetEpisodeLibraryItems(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string seasonId);

    IAsyncEnumerable<Tuple<JellyfinEpisode, int>> GetEpisodeLibraryItemsWithoutPeople(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string seasonId);

    IAsyncEnumerable<Tuple<JellyfinCollection, int>> GetCollectionLibraryItems(
        string address,
        string authorizationHeader,
        int mediaSourceId);

    IAsyncEnumerable<Tuple<MediaItem, int>> GetCollectionItems(
        string address,
        string authorizationHeader,
        int mediaSourceId,
        string collectionId);

    Task<Either<BaseError, MediaVersion>> GetPlaybackInfo(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string itemId);

    Task<Either<BaseError, Option<JellyfinShow>>> GetSingleShow(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string showId);

    Task<Either<BaseError, List<JellyfinShow>>> SearchShowsByTitle(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string showTitle);

    Task<Either<BaseError, Option<JellyfinEpisode>>> GetSingleEpisode(
        string address,
        string authorizationHeader,
        JellyfinLibrary library,
        string seasonId,
        string episodeId);

}
