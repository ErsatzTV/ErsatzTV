using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ITelevisionRepository
    {
        Task<bool> Update(Show show);
        Task<bool> Update(Season season);
        Task<bool> Update(Episode episode);
        Task<List<Show>> GetAllShows();
        Task<Option<Show>> GetShow(int showId);
        Task<int> GetShowCount();
        Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize);
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
        Task<Either<BaseError, Show>> AddShow(int libraryPathId, string showFolder, ShowMetadata metadata);
        Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber);
        Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path);
        Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath);
        Task<Unit> DeleteByPath(LibraryPath libraryPath, string path);
        Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath);
        Task<Unit> DeleteEmptyShows(LibraryPath libraryPath);
    }
}
