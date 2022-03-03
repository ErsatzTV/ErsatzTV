using System.Data;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class LibraryRepository : ILibraryRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public LibraryRepository(
        ILocalFileSystem localFileSystem,
        IDbContextFactory<TvContext> dbContextFactory,
        IDbConnection dbConnection)
    {
        _localFileSystem = localFileSystem;
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
            .ThenInclude(p => p.LibraryFolders)
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
        return context.Libraries
            .AsNoTracking()
            .Include(l => l.MediaSource)
            .Include(l => l.Paths)
            .ToListAsync();
    }

    public Task<Unit> UpdateLastScan(Library library) => _dbConnection.ExecuteAsync(
        "UPDATE Library SET LastScan = @LastScan WHERE Id = @Id",
        new { library.LastScan, library.Id }).ToUnit();

    public Task<Unit> UpdateLastScan(LibraryPath libraryPath) => _dbConnection.ExecuteAsync(
        "UPDATE LibraryPath SET LastScan = @LastScan WHERE Id = @Id",
        new { libraryPath.LastScan, libraryPath.Id }).ToUnit();

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

    public Task<List<int>> GetMediaIdsByLocalPath(int libraryPathId) =>
        _dbConnection.QueryAsync<int>(
                @"SELECT Id FROM MediaItem WHERE LibraryPathId = @LibraryPathId",
                new { LibraryPathId = libraryPathId })
            .Map(result => result.ToList());

    public async Task DeleteLocalPath(int libraryPathId)
    {
        await using TvContext context = _dbContextFactory.CreateDbContext();
        LibraryPath libraryPath = await context.LibraryPaths.FindAsync(libraryPathId);
        context.LibraryPaths.Remove(libraryPath);
        await context.SaveChangesAsync();
    }

    public Task<Unit> SetEtag(
        LibraryPath libraryPath,
        Option<LibraryFolder> knownFolder,
        string path,
        string etag) =>
        knownFolder.Match(
            async folder =>
            {
                await _dbConnection.ExecuteAsync(
                    "UPDATE LibraryFolder SET Etag = @Etag WHERE Id = @Id",
                    new { folder.Id, Etag = etag });
            },
            async () =>
            {
                await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
                await context.LibraryFolders.AddAsync(
                    new LibraryFolder
                    {
                        Path = path,
                        Etag = etag,
                        LibraryPathId = libraryPath.Id
                    });
                await context.SaveChangesAsync();
            }).ToUnit();

    public async Task<Unit> CleanEtagsForLibraryPath(LibraryPath libraryPath)
    {
        IEnumerable<string> folders = await _dbConnection.QueryAsync<string>(
            @"SELECT LF.Path
                FROM LibraryFolder LF
                WHERE LF.LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPath.Id });

        foreach (string folder in folders.Where(f => !_localFileSystem.FolderExists(f)))
        {
            await _dbConnection.ExecuteAsync(
                @"DELETE FROM LibraryFolder WHERE LibraryPathId = @LibraryPathId AND Path = @Path",
                new { LibraryPathId = libraryPath.Id, Path = folder });
        }

        return Unit.Default;
    }
}