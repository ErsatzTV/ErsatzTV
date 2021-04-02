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

        public async Task<Option<MusicVideo>> GetByMetadata(LibraryPath libraryPath, MusicVideoMetadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<int> maybeId = await dbContext.MusicVideoMetadata
                .Where(s => s.Artist == metadata.Artist && s.Title == metadata.Title && s.Year == metadata.Year)
                .Where(s => s.MusicVideo.LibraryPathId == libraryPath.Id)
                .SingleOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.MusicVideoId);

            return await maybeId.Match(
                id =>
                {
                    return dbContext.MusicVideos
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
                        .OrderBy(mv => mv.Id)
                        .SingleOrDefaultAsync(mv => mv.Id == id)
                        .Map(Optional);
                },
                () => Option<MusicVideo>.None.AsTask());
        }

        public async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> Add(
            LibraryPath libraryPath,
            string filePath,
            MusicVideoMetadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            try
            {
                metadata.DateAdded = DateTime.UtcNow;
                metadata.Genres ??= new List<Genre>();
                metadata.Tags ??= new List<Tag>();
                metadata.Studios ??= new List<Studio>();
                var musicVideo = new MusicVideo
                {
                    LibraryPathId = libraryPath.Id,
                    MusicVideoMetadata = new List<MusicVideoMetadata> { metadata },
                    MediaVersions = new List<MediaVersion>
                    {
                        new()
                        {
                            MediaFiles = new List<MediaFile>
                            {
                                new() { Path = filePath }
                            },
                            Streams = new List<MediaStream>()
                        }
                    }
                };

                await dbContext.MusicVideos.AddAsync(musicVideo);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(musicVideo).Reference(s => s.LibraryPath).LoadAsync();
                await dbContext.Entry(musicVideo.LibraryPath).Reference(lp => lp.Library).LoadAsync();

                return new MediaItemScanResult<MusicVideo>(musicVideo) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
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
    }
}
