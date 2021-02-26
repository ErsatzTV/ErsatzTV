using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMovieRepository
    {
        Task<Option<Movie>> GetMovie(int movieId);
        Task<Either<BaseError, Movie>> GetOrAdd(LibraryPath libraryPath, string path);
        Task<Either<BaseError, PlexMovie>> GetOrAdd(PlexLibrary library, PlexMovie item);
        Task<bool> Update(Movie movie);
        Task<int> GetMovieCount();
        Task<List<Movie>> GetPagedMovies(int pageNumber, int pageSize);
    }
}
