using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaServerTelevisionRepository<in TLibrary, TShow, TSeason, TEpisode, TEtag> where TLibrary : Library
    where TShow : Show
    where TSeason : Season
    where TEpisode : Episode
    where TEtag : MediaServerItemEtag
{
    Task<List<TEtag>> GetExistingShows(TLibrary library, CancellationToken cancellationToken);
    Task<List<TEtag>> GetExistingSeasons(TLibrary library, TShow show, CancellationToken cancellationToken);
    Task<List<TEtag>> GetExistingEpisodes(TLibrary library, TSeason season, CancellationToken cancellationToken);

    Task<Either<BaseError, MediaItemScanResult<TShow>>> GetOrAdd(
        TLibrary library,
        TShow item,
        CancellationToken cancellationToken);

    Task<Either<BaseError, MediaItemScanResult<TSeason>>> GetOrAdd(
        TLibrary library,
        TSeason item,
        CancellationToken cancellationToken);

    Task<Either<BaseError, MediaItemScanResult<TEpisode>>> GetOrAdd(
        TLibrary library,
        TEpisode item,
        bool deepScan,
        CancellationToken cancellationToken);

    Task<Unit> SetEtag(TShow show, string etag, CancellationToken cancellationToken);
    Task<Unit> SetEtag(TSeason season, string etag, CancellationToken cancellationToken);
    Task<Unit> SetEtag(TEpisode episode, string etag, CancellationToken cancellationToken);
    Task<Option<int>> FlagNormal(TLibrary library, TEpisode episode, CancellationToken cancellationToken);
    Task<Option<int>> FlagNormal(TLibrary library, TSeason season, CancellationToken cancellationToken);
    Task<Option<int>> FlagNormal(TLibrary library, TShow show, CancellationToken cancellationToken);
    Task<List<int>> FlagFileNotFoundShows(
        TLibrary library,
        List<string> showItemIds,
        CancellationToken cancellationToken);
    Task<List<int>> FlagFileNotFoundSeasons(
        TLibrary library,
        List<string> seasonItemIds,
        CancellationToken cancellationToken);
    Task<List<int>> FlagFileNotFoundEpisodes(
        TLibrary library,
        List<string> episodeItemIds,
        CancellationToken cancellationToken);
    Task<Option<int>> FlagUnavailable(TLibrary library, TEpisode episode, CancellationToken cancellationToken);
    Task<Option<int>> FlagRemoteOnly(TLibrary library, TEpisode episode, CancellationToken cancellationToken);
}
