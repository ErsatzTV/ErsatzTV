using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ITelevisionRepository
    {
        Task<bool> AllShowsExist(List<int> showIds);
        Task<List<Show>> GetAllShows();
        Task<Option<Show>> GetShow(int showId);
        Task<int> GetShowCount();
        Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize);
        Task<List<ShowMetadata>> GetShowsForCards(List<int> ids);
        Task<List<Episode>> GetShowItems(int showId);
        Task<List<Season>> GetAllSeasons();
        Task<Option<Season>> GetSeason(int seasonId);
        Task<int> GetSeasonCount(int showId);
        Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize);
        Task<List<Episode>> GetSeasonItems(int seasonId);
        Task<Option<Episode>> GetEpisode(int episodeId);
        Task<int> GetEpisodeCount(int seasonId);
        Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize);
        Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata);
        Task<Either<BaseError, MediaItemScanResult<Show>>> AddShow(int libraryPathId, string showFolder, ShowMetadata metadata);
        Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber);
        Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path);
        Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath);
        Task<Unit> DeleteByPath(LibraryPath libraryPath, string path);
        Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath);
        Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath);
        Task<Either<BaseError, MediaItemScanResult<PlexShow>>> GetOrAddPlexShow(PlexLibrary library, PlexShow item);
        Task<Either<BaseError, PlexSeason>> GetOrAddPlexSeason(PlexLibrary library, PlexSeason item);
        Task<Either<BaseError, PlexEpisode>> GetOrAddPlexEpisode(PlexLibrary library, PlexEpisode item);
        Task<bool> AddGenre(ShowMetadata metadata, Genre genre);
        Task<List<int>> RemoveMissingPlexShows(PlexLibrary library, List<string> showKeys);
        Task<Unit> RemoveMissingPlexSeasons(string showKey, List<string> seasonKeys);
        Task<Unit> RemoveMissingPlexEpisodes(string seasonKey, List<string> episodeKeys);
        Task<Unit> SetEpisodeNumber(Episode episode, int episodeNumber);
    }
}
