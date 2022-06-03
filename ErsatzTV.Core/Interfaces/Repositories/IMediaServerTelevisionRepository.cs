using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaServerTelevisionRepository<in TLibrary, TShow, TSeason, TEpisode, TEtag> where TLibrary : Library
    where TShow : Show
    where TSeason : Season
    where TEpisode : Episode
    where TEtag : MediaServerItemEtag
{
    Task<List<TEtag>> GetExistingShows(TLibrary library);
    Task<List<TEtag>> GetExistingSeasons(TLibrary library, TShow show);
    Task<List<TEtag>> GetExistingEpisodes(TLibrary library, TSeason season);
    Task<Either<BaseError, MediaItemScanResult<TShow>>> GetOrAdd(TLibrary library, TShow item);
    Task<Either<BaseError, MediaItemScanResult<TSeason>>> GetOrAdd(TLibrary library, TSeason item);
    Task<Either<BaseError, MediaItemScanResult<TEpisode>>> GetOrAdd(TLibrary library, TEpisode item);
    Task<Unit> SetEtag(TShow show, string etag);
    Task<Unit> SetEtag(TSeason season, string etag);
    Task<Unit> SetEtag(TEpisode episode, string etag);
    Task<bool> FlagNormal(TLibrary library, TEpisode episode);
    Task<bool> FlagNormal(TLibrary library, TSeason season);
    Task<bool> FlagNormal(TLibrary library, TShow show);
    Task<List<int>> FlagFileNotFoundShows(TLibrary library, List<string> showItemIds);
    Task<List<int>> FlagFileNotFoundSeasons(TLibrary library, List<string> seasonItemIds);
    Task<List<int>> FlagFileNotFoundEpisodes(TLibrary library, List<string> episodeItemIds);
    Task<Option<int>> FlagUnavailable(TLibrary library, TEpisode episode);
}
