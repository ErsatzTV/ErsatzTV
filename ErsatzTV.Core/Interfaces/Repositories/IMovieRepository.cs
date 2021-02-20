using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMovieRepository
    {
        Task<Option<MovieMediaItem>> GetMovie(int movieId);
        Task<Either<BaseError, MovieMediaItem>> GetOrAdd(int mediaSourceId, string path);
        Task<bool> Update(MovieMediaItem movie);
        Task<int> GetMovieCount();
        Task<List<MovieMediaItem>> GetPagedMovies(int pageNumber, int pageSize);
    }
}
