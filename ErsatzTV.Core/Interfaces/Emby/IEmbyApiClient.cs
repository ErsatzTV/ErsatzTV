using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyApiClient
{
    Task<Either<BaseError, EmbyServerInformation>> GetServerInformation(string address, string apiKey);
    Task<Either<BaseError, List<EmbyLibrary>>> GetLibraries(string address, string apiKey);

    IAsyncEnumerable<EmbyMovie> GetMovieLibraryItems(string address, string apiKey, EmbyLibrary library);

    IAsyncEnumerable<EmbyShow> GetShowLibraryItems(string address, string apiKey, EmbyLibrary library);

    IAsyncEnumerable<EmbySeason> GetSeasonLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId);

    IAsyncEnumerable<EmbyEpisode> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showId,
        string seasonId);

    IAsyncEnumerable<EmbyCollection> GetCollectionLibraryItems(string address, string apiKey);

    IAsyncEnumerable<MediaItem> GetCollectionItems(string address, string apiKey, string collectionId);

    Task<Either<BaseError, int>> GetLibraryItemCount(
        string address,
        string apiKey,
        string parentId,
        string includeItemTypes);
}
