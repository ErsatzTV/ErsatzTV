using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMovieRepository
{
    Task<bool> AllMoviesExist(List<int> movieIds);
    Task<Option<Movie>> GetMovie(int movieId);
    Task<Either<BaseError, MediaItemScanResult<Movie>>> GetOrAdd(LibraryPath libraryPath, string path);
    Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> GetOrAdd(PlexLibrary library, PlexMovie item);
    Task<List<MovieMetadata>> GetMoviesForCards(List<int> ids);
    Task<IEnumerable<string>> FindMoviePaths(LibraryPath libraryPath);
    Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
    Task<bool> AddGenre(MovieMetadata metadata, Genre genre);
    Task<bool> AddTag(MovieMetadata metadata, Tag tag);
    Task<bool> AddStudio(MovieMetadata metadata, Studio studio);
    Task<bool> AddActor(MovieMetadata metadata, Actor actor);
    Task<List<PlexItemEtag>> GetExistingPlexMovies(PlexLibrary library);
    Task<bool> UpdateSortTitle(MovieMetadata movieMetadata);
    Task<bool> AddDirector(MovieMetadata metadata, Director director);
    Task<bool> AddWriter(MovieMetadata metadata, Writer writer);
    Task<Unit> UpdatePath(int mediaFileId, string path);
    Task<Unit> SetPlexEtag(PlexMovie movie, string etag);
}
