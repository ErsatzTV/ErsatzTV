using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMovieRepository
    {
        public Task<Either<BaseError, MovieMediaItem>> GetOrAdd(int mediaSourceId, string path);
    }
}
