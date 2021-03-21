using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMovieRepository
    {
        Task<bool> AllMoviesExist(List<int> movieIds);
        Task<Option<Movie>> GetMovie(int movieId);
        Task<Either<BaseError, MediaItemScanResult<Movie>>> GetOrAdd(LibraryPath libraryPath, string path);
        Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> GetOrAdd(PlexLibrary library, PlexMovie item);
        Task<int> GetMovieCount();
        Task<List<MovieMetadata>> GetPagedMovies(int pageNumber, int pageSize);
        Task<List<MovieMetadata>> GetMoviesForCards(List<int> ids);
        Task<IEnumerable<string>> FindMoviePaths(LibraryPath libraryPath);
        Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
        Task<bool> AddGenre(MovieMetadata metadata, Genre genre);
        Task<bool> AddTag(MovieMetadata metadata, Tag tag);
        Task<bool> AddStudio(MovieMetadata metadata, Studio studio);
        Task<List<int>> RemoveMissingPlexMovies(PlexLibrary library, List<string> movieKeys);
    }
}
