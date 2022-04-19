using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class SongRepository : ISongRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SongRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, MediaItemScanResult<Song>>> GetOrAdd(
        LibraryPath libraryPath,
        string path)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<Song> maybeExisting = await dbContext.Songs
            .AsNoTracking()
            .Include(s => s.SongMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.SongMetadata)
            .ThenInclude(sm => sm.Genres)
            .Include(s => s.SongMetadata)
            .ThenInclude(sm => sm.Studios)
            .Include(s => s.SongMetadata)
            .ThenInclude(sm => sm.Tags)
            .Include(s => s.SongMetadata)
            .Include(s => s.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(s => s.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(s => s.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .OrderBy(s => s.MediaVersions.First().MediaFiles.First().Path)
            .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

        return await maybeExisting.Match(
            mediaItem =>
                Right<BaseError, MediaItemScanResult<Song>>(
                    new MediaItemScanResult<Song>(mediaItem) { IsAdded = false }).AsTask(),
            async () => await AddSong(dbContext, libraryPath.Id, path));
    }

    public async Task<IEnumerable<string>> FindSongPaths(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN Song O on MV.SongId = O.Id
                INNER JOIN MediaItem MI on O.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPath.Id });
    }

    public async Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT O.Id
            FROM Song O
            INNER JOIN MediaItem MI on O.Id = MI.Id
            INNER JOIN MediaVersion MV on O.Id = MV.SongId
            INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
            WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
            new { LibraryPathId = libraryPath.Id, Path = path }).Map(result => result.ToList());

        foreach (int songId in ids)
        {
            Song song = await dbContext.Songs.FindAsync(songId);
            if (song != null)
            {
                dbContext.Songs.Remove(song);
            }
        }

        await dbContext.SaveChangesAsync();

        return ids;
    }

    public async Task<bool> AddGenre(SongMetadata metadata, Genre genre)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Genre (Name, SongMetadataId) VALUES (@Name, @MetadataId)",
            new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<bool> AddTag(SongMetadata metadata, Tag tag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Tag (Name, SongMetadataId) VALUES (@Name, @MetadataId)",
            new { tag.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<List<SongMetadata>> GetSongsForCards(List<int> ids)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.SongMetadata
            .AsNoTracking()
            .Filter(ovm => ids.Contains(ovm.SongId))
            .Include(ovm => ovm.Song)
            .Include(ovm => ovm.Artwork)
            .Include(sm => sm.Song)
            .ThenInclude(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(ovm => ovm.SortTitle)
            .ToListAsync();
    }

    private static async Task<Either<BaseError, MediaItemScanResult<Song>>> AddSong(
        TvContext dbContext,
        int libraryPathId,
        string path)
    {
        try
        {
            var song = new Song
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

            await dbContext.Songs.AddAsync(song);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(song).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(song.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<Song>(song) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}
