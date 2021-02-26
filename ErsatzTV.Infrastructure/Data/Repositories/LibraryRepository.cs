using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public LibraryRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public Task<Option<Library>> Get(int libraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.Libraries
                .Include(l => l.Paths)
                .OrderBy(l => l.Id)
                .SingleOrDefaultAsync(l => l.Id == libraryId)
                .Map(Optional);
        }

        public Task<Unit> UpdateLastScan(Library library) => _dbConnection.ExecuteAsync(
            "UPDATE Library SET LastScan = @LastScan WHERE Id = @Id",
            new { library.LastScan, library.Id }).ToUnit();
    }
}
