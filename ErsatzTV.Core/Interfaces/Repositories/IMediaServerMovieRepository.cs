using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaServerMovieRepository<in TLibrary, TMovie, TEtag> where TLibrary : Library
    where TMovie : Movie
    where TEtag : MediaServerItemEtag
{
    Task<List<TEtag>> GetExistingMovies(TLibrary library);
    Task<bool> FlagNormal(TLibrary library, TMovie movie);
    Task<Option<int>> FlagUnavailable(TLibrary library, TMovie movie);
    Task<Option<int>> FlagRemoteOnly(TLibrary library, TMovie movie);
    Task<List<int>> FlagFileNotFound(TLibrary library, List<string> movieItemIds);
    Task<Either<BaseError, MediaItemScanResult<TMovie>>> GetOrAdd(TLibrary library, TMovie item);
    Task<Unit> SetEtag(TMovie movie, string etag);
}
