using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyApiClient
{
    Task<Either<BaseError, EmbyServerInformation>> GetServerInformation(string address, string apiKey);
    Task<Either<BaseError, List<EmbyLibrary>>> GetLibraries(string address, string apiKey);

    IAsyncEnumerable<Tuple<EmbyMovie, int>> GetMovieLibraryItems(string address, string apiKey, EmbyLibrary library);

    IAsyncEnumerable<Tuple<EmbyShow, int>> GetShowLibraryItems(string address, string apiKey, EmbyLibrary library);

    IAsyncEnumerable<Tuple<EmbySeason, int>> GetSeasonLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId);

    IAsyncEnumerable<Tuple<EmbyEpisode, int>> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId,
        string seasonId);

    IAsyncEnumerable<Tuple<EmbyCollection, int>> GetCollectionLibraryItems(string address, string apiKey);

    IAsyncEnumerable<Tuple<MediaItem, int>> GetCollectionItems(string address, string apiKey, string collectionId);

    Task<Either<BaseError, MediaVersion>> GetPlaybackInfo(
        string address,
        string apiKey,
        EmbyLibrary library,
        string itemId);

    Task<Either<BaseError, Option<EmbyShow>>> GetSingleShow(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId);

    Task<Either<BaseError, List<EmbyShow>>> SearchShowsByTitle(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showTitle);
}
