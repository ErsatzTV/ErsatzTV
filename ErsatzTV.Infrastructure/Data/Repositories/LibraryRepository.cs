using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
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
            .ThenInclude(lf => lf.ImageFolderDuration)
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

    public async Task SetEtag(
        LibraryPath libraryPath,
        Option<LibraryFolder> knownFolder,
        string path,
        string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (LibraryFolder folder in knownFolder)
        {
            await dbContext.Connection.ExecuteAsync(
                "UPDATE LibraryFolder SET Etag = @Etag WHERE Id = @Id",
                new { folder.Id, Etag = etag });
        }

        if (knownFolder.IsNone)
        {
            await dbContext.LibraryFolders.AddAsync(
                new LibraryFolder
                {
                    Path = path,
                    Etag = etag,
                    LibraryPathId = libraryPath.Id
                });

            await dbContext.SaveChangesAsync();
        }
    }

    public async Task CleanEtagsForLibraryPath(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        IOrderedEnumerable<LibraryFolder> orderedFolders = libraryPath.LibraryFolders
            .Where(f => !_localFileSystem.FolderExists(f.Path))
            .OrderByDescending(lp => lp.Path.Length);
        
        foreach (LibraryFolder folder in orderedFolders)
        {
            await dbContext.Connection.ExecuteAsync(
                """
                DELETE FROM LibraryFolder WHERE Id = @LibraryFolderId
                AND NOT EXISTS (SELECT Id FROM MediaFile WHERE LibraryFolderId = @LibraryFolderId)
                AND NOT EXISTS (SELECT Id FROM LibraryFolder WHERE ParentId = @LibraryFolderId)
                """,
                new { LibraryFolderId = folder.Id });
        }
    }

    public async Task<Option<int>> GetParentFolderId(string folder)
    {
        DirectoryInfo parent = new DirectoryInfo(folder).Parent;
        if (parent is null)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.LibraryFolders
            .AsNoTracking()
            .SelectOneAsync(lf => lf.Path, lf => lf.Path == parent.FullName)
            .MapT(lf => lf.Id);
    }

    public async Task<LibraryFolder> GetOrAddFolder(LibraryPath libraryPath, Option<int> maybeParentFolder, string folder)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        // load from db or create new folder
        LibraryFolder knownFolder = await libraryPath.LibraryFolders
            .Filter(f => f.Path == folder)
            .HeadOrNone()
            .IfNoneAsync(CreateNewFolder(libraryPath, maybeParentFolder, folder));

        // update parent folder if not present
        foreach (int parentFolder in maybeParentFolder)
        {
            if (knownFolder.ParentId != parentFolder)
            {
                knownFolder.ParentId = parentFolder;

                await dbContext.Connection.ExecuteAsync(
                    "UPDATE LibraryFolder SET ParentId = @ParentId WHERE Id = @Id",
                    new { ParentId = parentFolder, knownFolder.Id });
            }
        }

        // add new folder to library path
        if (knownFolder.Id < 1)
        {
            await dbContext.LibraryFolders.AddAsync(knownFolder);
            await dbContext.SaveChangesAsync();
        }
        
        return knownFolder;
    }

    public async Task UpdateLibraryFolderId(MediaFile mediaFile, int libraryFolderId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        mediaFile.LibraryFolderId = libraryFolderId;
        await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaFile SET LibraryFolderId = @LibraryFolderId WHERE Id = @Id",
            new { LibraryFolderId = libraryFolderId, mediaFile.Id });
    }

    public async Task UpdatePath(LibraryPath libraryPath, string normalizedLibraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        libraryPath.Path = normalizedLibraryPath;
        await dbContext.Connection.ExecuteAsync(
            "UPDATE LibraryPath SET Path = @Path WHERE Id = @Id",
            new { Path = normalizedLibraryPath, libraryPath.Id });
    }

    private static LibraryFolder CreateNewFolder(LibraryPath libraryPath, Option<int> maybeParentFolder, string folder)
    {
        int? parentId = null;
        foreach (int parentFolder in maybeParentFolder)
        {
            parentId = parentFolder;
        }
        
        return new LibraryFolder
        {
            Path = folder,
            Etag = null,
            LibraryPathId = libraryPath.Id,
            ParentId = parentId
        };
    }
}
