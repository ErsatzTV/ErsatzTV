using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class LibraryRepository : ILibraryRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;

    public LibraryRepository(ILocalFileSystem localFileSystem, IDbContextFactory<TvContext> dbContextFactory)
    {
        _localFileSystem = localFileSystem;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<LibraryPath> Add(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.LibraryPaths.AddAsync(libraryPath);
        await dbContext.SaveChangesAsync();
        return libraryPath;
    }

    public async Task<Option<Library>> GetLibrary(int libraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Libraries
            .Include(l => l.Paths)
            .ThenInclude(p => p.LibraryFolders)
            .OrderBy(l => l.Id)
            .SingleOrDefaultAsync(l => l.Id == libraryId)
            .Map(Optional);
    }

    public async Task<Option<LocalLibrary>> GetLocal(int libraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.LocalLibraries
            .OrderBy(l => l.Id)
            .SingleOrDefaultAsync(l => l.Id == libraryId)
            .Map(Optional);
    }

    public async Task<List<Library>> GetAll()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Libraries
            .AsNoTracking()
            .Include(l => l.MediaSource)
            .Include(l => l.Paths)
            .ToListAsync();
    }

    public async Task<Unit> UpdateLastScan(Library library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE Library SET LastScan = @LastScan WHERE Id = @Id",
            new { library.LastScan, library.Id }).ToUnit();
    }

    public async Task<Unit> UpdateLastScan(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE LibraryPath SET LastScan = @LastScan WHERE Id = @Id",
            new { libraryPath.LastScan, libraryPath.Id }).ToUnit();
    }

    public async Task<List<LibraryPath>> GetLocalPaths(int libraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.LocalLibraries
            .Include(l => l.Paths)
            .OrderBy(l => l.Id)
            .SingleOrDefaultAsync(l => l.Id == libraryId)
            .Map(Optional)
            .Match(l => l.Paths, () => new List<LibraryPath>());
    }

    public async Task<int> CountMediaItemsByPath(int libraryPathId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QuerySingleAsync<int>(
            @"SELECT COUNT(*) FROM MediaItem WHERE LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPathId });
    }

    public async Task<Unit> SetEtag(
        LibraryPath libraryPath,
        Option<LibraryFolder> knownFolder,
        string path,
        string etag) =>
        await knownFolder.Match(
            async folder =>
            {
                await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
                await dbContext.Connection.ExecuteAsync(
                    "UPDATE LibraryFolder SET Etag = @Etag WHERE Id = @Id",
                    new { folder.Id, Etag = etag });
            },
            async () =>
            {
                await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
                await dbContext.LibraryFolders.AddAsync(
                    new LibraryFolder
                    {
                        Path = path,
                        Etag = etag,
                        LibraryPathId = libraryPath.Id
                    });
                await dbContext.SaveChangesAsync();
            }).ToUnit();

    public async Task<Unit> CleanEtagsForLibraryPath(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        IEnumerable<string> folders = await dbContext.Connection.QueryAsync<string>(
            @"SELECT LF.Path
                FROM LibraryFolder LF
                WHERE LF.LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPath.Id });

        foreach (string folder in folders.Where(f => !_localFileSystem.FolderExists(f)))
        {
            await dbContext.Connection.ExecuteAsync(
                @"DELETE FROM LibraryFolder WHERE LibraryPathId = @LibraryPathId AND Path = @Path",
                new { LibraryPathId = libraryPath.Id, Path = folder });
        }

        return Unit.Default;
    }
}
