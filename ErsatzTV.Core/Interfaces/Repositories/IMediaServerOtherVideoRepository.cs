using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaServerOtherVideoRepository<in TLibrary, TOtherVideo, TEtag> where TLibrary : Library
    where TOtherVideo : OtherVideo
    where TEtag : MediaServerItemEtag
{
    Task<List<TEtag>> GetExistingOtherVideos(TLibrary library);
    Task<Option<int>> FlagNormal(TLibrary library, TOtherVideo otherVideo);
    Task<Option<int>> FlagUnavailable(TLibrary library, TOtherVideo otherVideo);
    Task<Option<int>> FlagRemoteOnly(TLibrary library, TOtherVideo otherVideo);
    Task<List<int>> FlagFileNotFound(TLibrary library, List<string> movieItemIds);

    Task<Either<BaseError, MediaItemScanResult<TOtherVideo>>> GetOrAdd(
        TLibrary library,
        TOtherVideo item,
        bool deepScan,
        CancellationToken cancellationToken);

    Task<Unit> SetEtag(TOtherVideo otherVideo, string etag);
}
