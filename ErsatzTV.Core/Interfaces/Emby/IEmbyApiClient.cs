using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyApiClient
{
    Task<Either<BaseError, EmbyServerInformation>> GetServerInformation(string address, string apiKey);
    Task<Either<BaseError, List<EmbyLibrary>>> GetLibraries(string address, string apiKey);

    IAsyncEnumerable<EmbyMovie> GetMovieLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library);

    Task<Either<BaseError, List<EmbyShow>>> GetShowLibraryItems(
        string address,
        string apiKey,
        string libraryId);

    Task<Either<BaseError, List<EmbySeason>>> GetSeasonLibraryItems(
        string address,
        string apiKey,
        string showId);

    Task<Either<BaseError, List<EmbyEpisode>>> GetEpisodeLibraryItems(
        string address,
        string apiKey,
        EmbyLibrary library,
        string seasonId);

    Task<Either<BaseError, List<EmbyCollection>>> GetCollectionLibraryItems(
        string address,
        string apiKey);

    Task<Either<BaseError, List<MediaItem>>> GetCollectionItems(
        string address,
        string apiKey,
        string collectionId);

    Task<Either<BaseError, int>> GetLibraryItemCount(
        string address,
        string apiKey,
        EmbyLibrary library,
        string includeItemTypes);
}
