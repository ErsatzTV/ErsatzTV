using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class OtherVideoRepository : IOtherVideoRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public OtherVideoRepository(IDbConnection dbConnection, IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbConnection = dbConnection;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> GetOrAdd(
        LibraryPath libraryPath,
        string path)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Option<OtherVideo> maybeExisting = await dbContext.OtherVideos
            .AsNoTracking()
            .Include(ov => ov.OtherVideoMetadata)
            .ThenInclude(ovm => ovm.Artwork)
            .Include(ov => ov.OtherVideoMetadata)
            .ThenInclude(ovm => ovm.Tags)
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
                Right<BaseError, MediaItemScanResult<OtherVideo>>(
                    new MediaItemScanResult<OtherVideo>(mediaItem) { IsAdded = false }).AsTask(),
            async () => await AddOtherVideo(dbContext, libraryPath.Id, path));
    }

    public Task<IEnumerable<string>> FindOtherVideoPaths(LibraryPath libraryPath) =>
        _dbConnection.QueryAsync<string>(
            @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN OtherVideo O on MV.OtherVideoId = O.Id
                INNER JOIN MediaItem MI on O.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPath.Id });

    public async Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path)
    {
        List<int> ids = await _dbConnection.QueryAsync<int>(
            @"SELECT O.Id
            FROM OtherVideo O
            INNER JOIN MediaItem MI on O.Id = MI.Id
            INNER JOIN MediaVersion MV on O.Id = MV.OtherVideoId
            INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
            WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
            new { LibraryPathId = libraryPath.Id, Path = path }).Map(result => result.ToList());

        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        foreach (int otherVideoId in ids)
        {
            OtherVideo otherVideo = await dbContext.OtherVideos.FindAsync(otherVideoId);
            dbContext.OtherVideos.Remove(otherVideo);
        }

        await dbContext.SaveChangesAsync();

        return ids;
    }

    public Task<bool> AddTag(OtherVideoMetadata metadata, Tag tag) =>
        _dbConnection.ExecuteAsync(
            "INSERT INTO Tag (Name, OtherVideoMetadataId) VALUES (@Name, @MetadataId)",
            new { tag.Name, MetadataId = metadata.Id }).Map(result => result > 0);

    public async Task<List<OtherVideoMetadata>> GetOtherVideosForCards(List<int> ids)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.OtherVideoMetadata
            .AsNoTracking()
            .Filter(ovm => ids.Contains(ovm.OtherVideoId))
            .Include(ovm => ovm.OtherVideo)
            .Include(ovm => ovm.Artwork)
            .Include(ovm => ovm.OtherVideo)
            .ThenInclude(ov => ov.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(ovm => ovm.SortTitle)
            .ToListAsync();
    }

    private static async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> AddOtherVideo(
        TvContext dbContext,
        int libraryPathId,
        string path)
    {
        try
        {
            var otherVideo = new OtherVideo
            {
                LibraryPathId = libraryPathId,
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = path }
                        },
                        Streams = new List<MediaStream>()
                    }
                },
                TraktListItems = new List<TraktListItem>()
            };

            await dbContext.OtherVideos.AddAsync(otherVideo);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(otherVideo).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(otherVideo.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<OtherVideo>(otherVideo) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}