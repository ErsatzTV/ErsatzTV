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
        Task<Option<Show>> GetShow(int televisionShowId);
        Task<int> GetShowCount();
        Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize);
        Task<List<Episode>> GetShowItems(int showId);
        Task<List<Season>> GetAllSeasons();
        Task<Option<Season>> GetSeason(int televisionSeasonId);
        Task<int> GetSeasonCount(int televisionShowId);
        Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize);
        Task<List<Episode>> GetSeasonItems(int seasonId);
        Task<Option<Episode>> GetEpisode(int televisionEpisodeId);
        Task<int> GetEpisodeCount(int televisionSeasonId);
        Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize);
        Task<Option<Show>> GetShowByMetadata(ShowMetadata metadata);

        Task<Either<BaseError, Show>> AddShow(
            int localMediaSourceId,
            string showFolder,
            ShowMetadata metadata);

        Task<Either<BaseError, Season>> GetOrAddSeason(
            Show show,
            string path,
            int seasonNumber);

        Task<Either<BaseError, Episode>> GetOrAddEpisode(
            Season season,
            LibraryPath libraryPath,
            string path);

        Task<Unit> DeleteEmptyShows();
    }
}
