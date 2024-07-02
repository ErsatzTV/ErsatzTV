using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexServerApiClient
{
    Task<bool> Ping(
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, List<PlexLibrary>>> GetLibraries(
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<Tuple<PlexMovie, int>> GetMovieLibraryContents(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<Tuple<PlexShow, int>> GetShowLibraryContents(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<Tuple<PlexSeason, int>> GetShowSeasons(
        PlexLibrary library,
        PlexShow show,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<Tuple<PlexEpisode, int>> GetSeasonEpisodes(
        PlexLibrary library,
        PlexSeason season,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, ShowMetadata>> GetShowMetadata(
        PlexLibrary library,
        string key,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, Tuple<MovieMetadata, MediaVersion>>> GetMovieMetadataAndStatistics(
        int plexMediaSourceId,
        string key,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, Tuple<EpisodeMetadata, MediaVersion>>> GetEpisodeMetadataAndStatistics(
        int plexMediaSourceId,
        string key,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<Tuple<PlexCollection, int>> GetAllCollections(
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken);

    IAsyncEnumerable<Tuple<MediaItem, int>> GetCollectionItems(
        PlexConnection connection,
        PlexServerAuthToken token,
        string key,
        CancellationToken cancellationToken);
}
