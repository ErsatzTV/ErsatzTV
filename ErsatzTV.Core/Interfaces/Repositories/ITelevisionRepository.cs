using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ITelevisionRepository
    {
        Task<List<TelevisionShow>> GetAllByMediaSourceId(int mediaSourceId);
        Task<bool> Update(TelevisionShow show);
        Task<bool> Update(TelevisionSeason season);
        Task<bool> Update(TelevisionEpisodeMediaItem episode);
        Task<Option<TelevisionShow>> GetShow(int televisionShowId);
        Task<int> GetShowCount();
        Task<List<TelevisionShow>> GetPagedShows(int pageNumber, int pageSize);
        Task<int> GetSeasonCount(int televisionShowId);
        Task<List<TelevisionSeason>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize);
        Task<Either<BaseError, TelevisionShow>> GetOrAddShow(int mediaSourceId, string path);

        Task<Either<BaseError, TelevisionSeason>> GetOrAddSeason(
            TelevisionShow show,
            string path,
            int seasonNumber);

        Task<Either<BaseError, TelevisionEpisodeMediaItem>> GetOrAddEpisode(
            TelevisionSeason season,
            int mediaSourceId,
            string path);

        Task<List<TelevisionShow>> FindRemovedShows(LocalMediaSource localMediaSource, List<string> allShowFolders);
        Task<Unit> Delete(TelevisionShow show);
    }
}
