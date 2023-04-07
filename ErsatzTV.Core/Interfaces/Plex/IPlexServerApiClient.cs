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

    IAsyncEnumerable<PlexMovie> GetMovieLibraryContents(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<PlexShow> GetShowLibraryContents(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, int>> CountShowSeasons(
        PlexShow show,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<PlexSeason> GetShowSeasons(
        PlexLibrary library,
        PlexShow show,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, int>> CountSeasonEpisodes(
        PlexSeason season,
        PlexConnection connection,
        PlexServerAuthToken token);

    IAsyncEnumerable<PlexEpisode> GetSeasonEpisodes(
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
        PlexLibrary library,
        string key,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, Tuple<EpisodeMetadata, MediaVersion>>> GetEpisodeMetadataAndStatistics(
        PlexLibrary library,
        string key,
        PlexConnection connection,
        PlexServerAuthToken token);

    Task<Either<BaseError, int>> GetLibraryItemCount(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token);
}
