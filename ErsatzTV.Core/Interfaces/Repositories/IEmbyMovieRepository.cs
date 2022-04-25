using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IEmbyMovieRepository
{
    Task<bool> FlagNormal(EmbyLibrary library, EmbyMovie movie);
    Task<Option<int>> FlagUnavailable(EmbyLibrary library, EmbyMovie movie);
    Task<List<int>> FlagFileNotFound(EmbyLibrary library, List<string> embyMovieItemIds);
    Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> GetOrAdd(EmbyLibrary library, EmbyMovie item);
    Task<Unit> SetEtag(EmbyMovie movie, string etag);
}
