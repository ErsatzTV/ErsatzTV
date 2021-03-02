using System.Collections.Generic;
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

        public async Task<LibraryPath> Add(LibraryPath libraryPath)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            await context.LibraryPaths.AddAsync(libraryPath);
            await context.SaveChangesAsync();
            return libraryPath;
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

        public Task<Option<LocalLibrary>> GetLocal(int libraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.LocalLibraries
                .OrderBy(l => l.Id)
                .SingleOrDefaultAsync(l => l.Id == libraryId)
                .Map(Optional);
        }

        public Task<List<Library>> GetAll()
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.Libraries.ToListAsync();
        }

        public Task<Unit> UpdateLastScan(Library library) => _dbConnection.ExecuteAsync(
            "UPDATE Library SET LastScan = @LastScan WHERE Id = @Id",
            new { library.LastScan, library.Id }).ToUnit();

        public Task<List<LibraryPath>> GetLocalPaths(int libraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.LocalLibraries
                .Include(l => l.Paths)
                .OrderBy(l => l.Id)
                .SingleOrDefaultAsync(l => l.Id == libraryId)
                .Map(Optional)
                .Match(l => l.Paths, () => new List<LibraryPath>());
        }

        public Task<Option<LibraryPath>> GetPath(int libraryPathId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.LibraryPaths
                .OrderBy(lp => lp.Id)
                .SingleOrDefaultAsync(lp => lp.Id == libraryPathId)
                .Map(Optional);
        }

        public Task<int> CountMediaItemsByPath(int libraryPathId) =>
            _dbConnection.QuerySingleAsync<int>(
                @"SELECT COUNT(*) FROM MediaItem WHERE LibraryPathId = @LibraryPathId",
                new { LibraryPathId = libraryPathId });

        public async Task DeleteLocalPath(int libraryPathId)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            LibraryPath libraryPath = await context.LibraryPaths.FindAsync(libraryPathId);
            context.LibraryPaths.Remove(libraryPath);
            await context.SaveChangesAsync();
        }
    }
}
