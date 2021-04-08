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

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MusicVideoRepository : IMusicVideoRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MusicVideoRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> GetOrAdd(
            Artist artist,
            LibraryPath libraryPath,
            string path)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<MusicVideo> maybeExisting = await dbContext.MusicVideos
                .AsNoTracking()
                .Include(mv => mv.MusicVideoMetadata)
                .ThenInclude(mvm => mvm.Artwork)
                .Include(mv => mv.MusicVideoMetadata)
                .ThenInclude(mvm => mvm.Genres)
                .Include(mv => mv.MusicVideoMetadata)
                .ThenInclude(mvm => mvm.Tags)
                .Include(mv => mv.MusicVideoMetadata)
                .ThenInclude(mvm => mvm.Studios)
                .Include(mv => mv.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .Include(mv => mv.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(mv => mv.MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
                .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

            return await maybeExisting.Match(
                async mediaItem =>
                {
                    if (mediaItem.ArtistId != artist.Id)
                    {
                        await _dbConnection.ExecuteAsync(
                            @"UPDATE MusicVideo SET ArtistId = @ArtistId WHERE Id = @Id",
                            new { mediaItem.Id, ArtistId = artist.Id });

                        mediaItem.ArtistId = artist.Id;
                        mediaItem.Artist = artist;
                    }

                    return Right<BaseError, MediaItemScanResult<MusicVideo>>(
                        new MediaItemScanResult<MusicVideo>(mediaItem) { IsAdded = false });
                },
                async () => await AddMusicVideo(dbContext, artist, libraryPath.Id, path));
        }

        public Task<IEnumerable<string>> FindMusicVideoPaths(LibraryPath libraryPath) =>
            _dbConnection.QueryAsync<string>(
                @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN MusicVideo M on MV.MusicVideoId = M.Id
                INNER JOIN MediaItem MI on M.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
                new { LibraryPathId = libraryPath.Id });

        public async Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MusicVideo M
                INNER JOIN MediaItem MI on M.Id = MI.Id
                INNER JOIN MediaVersion MV on M.Id = MV.EpisodeId
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
                new { LibraryPathId = libraryPath.Id, Path = path }).Map(result => result.ToList());

            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            foreach (int musicVideoId in ids)
            {
                MusicVideo musicVideo = await dbContext.MusicVideos.FindAsync(musicVideoId);
                dbContext.MusicVideos.Remove(musicVideo);
            }

            await dbContext.SaveChangesAsync();

            return ids;
        }

        public Task<bool> AddGenre(MusicVideoMetadata metadata, Genre genre) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Genre (Name, MusicVideoMetadataId) VALUES (@Name, @MetadataId)",
                new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddTag(MusicVideoMetadata metadata, Tag tag) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Tag (Name, MusicVideoMetadataId) VALUES (@Name, @MetadataId)",
                new { tag.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddStudio(MusicVideoMetadata metadata, Studio studio) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Studio (Name, MusicVideoMetadataId) VALUES (@Name, @MetadataId)",
                new { studio.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public async Task<List<MusicVideoMetadata>> GetMusicVideosForCards(List<int> ids)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.MusicVideoMetadata
                .AsNoTracking()
                .Filter(mvm => ids.Contains(mvm.MusicVideoId))
                .Include(mvm => mvm.Artwork)
                .OrderBy(mvm => mvm.SortTitle)
                .ToListAsync();
        }

        public async Task<Option<MusicVideo>> GetMusicVideo(int musicVideoId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.MusicVideos
                .Include(m => m.MusicVideoMetadata)
                .ThenInclude(m => m.Artwork)
                .Include(m => m.MusicVideoMetadata)
                .ThenInclude(m => m.Genres)
                .Include(m => m.MusicVideoMetadata)
                .ThenInclude(m => m.Tags)
                .Include(m => m.MusicVideoMetadata)
                .ThenInclude(m => m.Studios)
                .OrderBy(m => m.Id)
                .SingleOrDefaultAsync(m => m.Id == musicVideoId)
                .Map(Optional);
        }

        public Task<IEnumerable<string>> FindOrphanPaths(LibraryPath libraryPath) =>
            _dbConnection.QueryAsync<string>(
                @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN MusicVideo M on MV.MusicVideoId = M.Id
                INNER JOIN MediaItem MI on M.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId
                  AND NOT EXISTS (SELECT * FROM MusicVideoMetadata WHERE MusicVideoId = M.Id)",
                new { LibraryPathId = libraryPath.Id });

        private static async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> AddMusicVideo(
            TvContext dbContext,
            Artist artist,
            int libraryPathId,
            string path)
        {
            try
            {
                var musicVideo = new MusicVideo
                {
                    ArtistId = artist.Id,
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
                    }
                };

                await dbContext.MusicVideos.AddAsync(musicVideo);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(musicVideo).Reference(m => m.LibraryPath).LoadAsync();
                await dbContext.Entry(musicVideo.LibraryPath).Reference(lp => lp.Library).LoadAsync();
                return new MediaItemScanResult<MusicVideo>(musicVideo) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
