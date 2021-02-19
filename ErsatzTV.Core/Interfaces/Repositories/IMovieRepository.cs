using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMovieRepository
    {
        public Task<Either<BaseError, MovieMediaItem>> GetOrAdd(int mediaSourceId, string path);
        public Task<bool> Update(MovieMediaItem movie);
        public Task<int> GetMovieCount();
        public Task<List<MovieMediaItem>> GetPagedMovies(int pageNumber, int pageSize);
    }
}
