using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ITelevisionRepository
    {
        Task<bool> Update(TelevisionShow show);
        Task<bool> Update(TelevisionSeason season);
        Task<bool> Update(TelevisionEpisodeMediaItem episode);
        Task<List<TelevisionShow>> GetAllShows();
        Task<Option<TelevisionShow>> GetShow(int televisionShowId);
        Task<int> GetShowCount();
        Task<List<TelevisionShow>> GetPagedShows(int pageNumber, int pageSize);
        Task<List<TelevisionEpisodeMediaItem>> GetShowItems(int televisionShowId);
        Task<List<TelevisionSeason>> GetAllSeasons();
        Task<Option<TelevisionSeason>> GetSeason(int televisionSeasonId);
        Task<int> GetSeasonCount(int televisionShowId);
        Task<List<TelevisionSeason>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize);
        Task<List<TelevisionEpisodeMediaItem>> GetSeasonItems(int televisionSeasonId);
        Task<Option<TelevisionEpisodeMediaItem>> GetEpisode(int televisionEpisodeId);
        Task<int> GetEpisodeCount(int televisionSeasonId);
        Task<List<TelevisionEpisodeMediaItem>> GetPagedEpisodes(int televisionSeasonId, int pageNumber, int pageSize);
        Task<Option<TelevisionShow>> GetShowByPath(int mediaSourceId, string path);
        Task<Option<TelevisionShow>> GetShowByMetadata(TelevisionShowMetadata metadata);

        Task<Either<BaseError, TelevisionShow>> AddShow(
            int localMediaSourceId,
            string showFolder,
            TelevisionShowMetadata metadata);

        Task<Either<BaseError, TelevisionSeason>> GetOrAddSeason(
            TelevisionShow show,
            string path,
            int seasonNumber);

        Task<Either<BaseError, TelevisionEpisodeMediaItem>> GetOrAddEpisode(
            TelevisionSeason season,
            int mediaSourceId,
            string path);

        Task<Unit> DeleteMissingSources(int localMediaSourceId, List<string> allFolders);
        Task<Unit> DeleteEmptyShows();
    }
}
