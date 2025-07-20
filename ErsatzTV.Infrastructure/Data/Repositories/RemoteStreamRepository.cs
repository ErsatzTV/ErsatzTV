using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class RemoteStreamRepository(
    IDbContextFactory<TvContext> dbContextFactory,
    ILogger<RemoteStreamRepository> logger)
    : IRemoteStreamRepository
{
    public async Task<Either<BaseError, MediaItemScanResult<RemoteStream>>> GetOrAdd(
        LibraryPath libraryPath,
        LibraryFolder libraryFolder,
        string path)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Option<RemoteStream> maybeExisting = await dbContext.RemoteStreams
            .AsNoTracking()
            .Include(i => i.RemoteStreamMetadata)
            .ThenInclude(ovm => ovm.Genres)
            .Include(i => i.RemoteStreamMetadata)
            .ThenInclude(ovm => ovm.Tags)
            .Include(i => i.RemoteStreamMetadata)
            .ThenInclude(ovm => ovm.Studios)
            .Include(i => i.RemoteStreamMetadata)
            .ThenInclude(ovm => ovm.Guids)
            .Include(i => i.RemoteStreamMetadata)
            .ThenInclude(ovm => ovm.Actors)
            .Include(i => i.RemoteStreamMetadata)
            .ThenInclude(ovm => ovm.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(ov => ov.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(ov => ov.MediaVersions)
            .ThenInclude(ov => ov.MediaFiles)
            .Include(ov => ov.MediaVersions)
            .ThenInclude(ov => ov.Streams)
            .Include(ov => ov.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
            .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

        return await maybeExisting.Match(
            mediaItem =>
                Right<BaseError, MediaItemScanResult<RemoteStream>>(
                    new MediaItemScanResult<RemoteStream>(mediaItem) { IsAdded = false }).AsTask(),
            async () => await AddRemoteStream(dbContext, libraryPath.Id, libraryFolder.Id, path));
    }

    public async Task<IEnumerable<string>> FindRemoteStreamPaths(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN RemoteStream O on MV.RemoteStreamId = O.Id
                INNER JOIN MediaItem MI on O.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPath.Id });
    }

    public async Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT O.Id
            FROM RemoteStream O
            INNER JOIN MediaItem MI on O.Id = MI.Id
            INNER JOIN MediaVersion MV on O.Id = MV.RemoteStreamId
            INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
            WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
            new { LibraryPathId = libraryPath.Id, Path = path }).Map(result => result.ToList());

        foreach (int remoteStreamId in ids)
        {
            RemoteStream remoteStream = await dbContext.RemoteStreams.FindAsync(remoteStreamId);
            if (remoteStream != null)
            {
                dbContext.RemoteStreams.Remove(remoteStream);
            }
        }

        await dbContext.SaveChangesAsync();

        return ids;
    }

    public async Task<bool> AddTag(RemoteStreamMetadata metadata, Tag tag)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Tag (Name, RemoteStreamMetadataId, ExternalCollectionId) VALUES (@Name, @MetadataId, @ExternalCollectionId)",
            new { tag.Name, MetadataId = metadata.Id, tag.ExternalCollectionId }).Map(result => result > 0);
    }

    public async Task<List<RemoteStreamMetadata>> GetRemoteStreamsForCards(List<int> ids)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.RemoteStreamMetadata
            .AsNoTracking()
            .Filter(im => ids.Contains(im.RemoteStreamId))
            .Include(im => im.RemoteStream)
            .Include(im => im.Artwork)
            .Include(im => im.RemoteStream)
            .ThenInclude(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(im => im.SortTitle)
            .ToListAsync();
    }

    private async Task<Either<BaseError, MediaItemScanResult<RemoteStream>>> AddRemoteStream(
        TvContext dbContext,
        int libraryPathId,
        int libraryFolderId,
        string path)
    {
        try
        {
            if (await MediaItemRepository.MediaFileAlreadyExists(path, libraryPathId, dbContext, logger))
            {
                return new MediaFileAlreadyExists();
            }

            var remoteStream = new RemoteStream
            {
                LibraryPathId = libraryPathId,
                MediaVersions =
                [
                    new MediaVersion
                    {
                        MediaFiles = [new MediaFile { Path = path, LibraryFolderId = libraryFolderId }],
                        Streams = []
                    }
                ],
                TraktListItems = new List<TraktListItem>
                {
                    Capacity = 0
                }
            };

            await dbContext.RemoteStreams.AddAsync(remoteStream);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(remoteStream).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(remoteStream.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<RemoteStream>(remoteStream) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}
