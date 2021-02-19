using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ITelevisionRepository
    {
        public Task<Unit> Add(TelevisionShow show);
        public Task<List<TelevisionShow>> GetAllByMediaSourceId(int mediaSourceId);
        public Task<bool> Update(TelevisionShow show);
        public Task<bool> Update(TelevisionSeason season);
        public Task<bool> Update(TelevisionEpisodeMediaItem episode);
        public Task<int> GetShowCount();
        public Task<List<TelevisionShow>> GetPagedShows(int pageNumber, int pageSize);
        public Task<int> GetSeasonCount(int televisionShowId);
        public Task<List<TelevisionSeason>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize);
        public Task<Either<BaseError, TelevisionShow>> GetOrAddShow(int mediaSourceId, string path);
        public Task<Either<BaseError, TelevisionSeason>> GetOrAddSeason(
            TelevisionShow show,
            string path,
            int seasonNumber);
        public Task<Either<BaseError, TelevisionEpisodeMediaItem>> GetOrAddEpisode(
            TelevisionSeason season,
            int mediaSourceId,
            string path);
    }
}
