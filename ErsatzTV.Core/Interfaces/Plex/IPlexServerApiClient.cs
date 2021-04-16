using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexServerApiClient
    {
        Task<Either<BaseError, List<PlexLibrary>>> GetLibraries(
            PlexConnection connection,
            PlexServerAuthToken token);

        Task<Either<BaseError, List<PlexMovie>>> GetMovieLibraryContents(
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token);
        
        Task<Either<BaseError, List<PlexShow>>> GetShowLibraryContents(
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token);

        Task<Either<BaseError, List<PlexSeason>>> GetShowSeasons(
            PlexLibrary library,
            PlexShow show,
            PlexConnection connection,
            PlexServerAuthToken token);

        Task<Either<BaseError, List<PlexEpisode>>> GetSeasonEpisodes(
            PlexLibrary library,
            PlexSeason season,
            PlexConnection connection,
            PlexServerAuthToken token);
        
        Task<Either<BaseError, MovieMetadata>> GetMovieMetadata(
            PlexLibrary library,
            string key,
            PlexConnection connection,
            PlexServerAuthToken token);

        Task<Either<BaseError, ShowMetadata>> GetShowMetadata(
            PlexLibrary library,
            string key,
            PlexConnection connection,
            PlexServerAuthToken token);

        Task<Either<BaseError, MediaVersion>> GetStatistics(
            string key,
            PlexConnection connection,
            PlexServerAuthToken token);
    }
}
