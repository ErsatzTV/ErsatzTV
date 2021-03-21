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
    public class TelevisionRepository : ITelevisionRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public TelevisionRepository(IDbConnection dbConnection, IDbContextFactory<TvContext> dbContextFactory)
        {
            _dbConnection = dbConnection;
            _dbContextFactory = dbContextFactory;
        }

        public Task<bool> AllShowsExist(List<int> showIds) =>
            _dbConnection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM Show WHERE Id in @ShowIds",
                    new { ShowIds = showIds })
                .Map(c => c == showIds.Count);

        public async Task<List<Show>> GetAllShows()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Shows
                .AsNoTracking()
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .ToListAsync();
        }

        public async Task<Option<Show>> GetShow(int showId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Shows
                .AsNoTracking()
                .Filter(s => s.Id == showId)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Genres)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Tags)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Studios)
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync()
                .Map(Optional);
        }

        public async Task<int> GetShowCount()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ShowMetadata
                .AsNoTracking()
                .GroupBy(sm => new { sm.Title, sm.Year })
                .CountAsync();
        }

        public async Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ShowMetadata.FromSqlRaw(
                    @"SELECT * FROM ShowMetadata WHERE Id IN
            (SELECT MIN(Id) FROM ShowMetadata GROUP BY Title, Year, MetadataKind HAVING MetadataKind = MAX(MetadataKind))
            ORDER BY SortTitle
            LIMIT {0} OFFSET {1}",
                    pageSize,
                    (pageNumber - 1) * pageSize)
                .AsNoTracking()
                .Include(mm => mm.Artwork)
                .OrderBy(mm => mm.SortTitle)
                .ToListAsync();
        }

        public async Task<List<ShowMetadata>> GetShowsForCards(List<int> ids)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ShowMetadata
                .AsNoTracking()
                .Filter(sm => ids.Contains(sm.ShowId))
                .Include(sm => sm.Artwork)
                .OrderBy(sm => sm.SortTitle)
                .ToListAsync();
        }

        public async Task<List<Season>> GetAllSeasons()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Seasons
                .AsNoTracking()
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .ToListAsync();
        }

        public async Task<Option<Season>> GetSeason(int seasonId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Seasons
                .AsNoTracking()
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync(s => s.Id == seasonId)
                .Map(Optional);
        }

        public async Task<int> GetSeasonCount(int showId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Seasons
                .AsNoTracking()
                .CountAsync(s => s.ShowId == showId);
        }

        public async Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize)
        {
            List<int> showIds = await _dbConnection.QueryAsync<int>(
                    @"SELECT m1.ShowId
                FROM ShowMetadata m1
                LEFT OUTER JOIN ShowMetadata m2 ON m2.ShowId = @ShowId
                WHERE m1.Title = m2.Title AND m1.Year = m2.Year",
                    new { ShowId = televisionShowId })
                .Map(results => results.ToList());

            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Seasons
                .AsNoTracking()
                .Where(s => showIds.Contains(s.ShowId))
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .OrderBy(s => s.SeasonNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Option<Episode>> GetEpisode(int episodeId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Episodes
                .AsNoTracking()
                .Include(e => e.Season)
                .Include(e => e.EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync(s => s.Id == episodeId)
                .Map(Optional);
        }

        public async Task<int> GetEpisodeCount(int seasonId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Episodes
                .AsNoTracking()
                .CountAsync(e => e.SeasonId == seasonId);
        }

        public async Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.EpisodeMetadata
                .AsNoTracking()
                .Filter(em => em.Episode.SeasonId == seasonId)
                .Include(em => em.Artwork)
                .Include(em => em.Episode)
                .ThenInclude(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .OrderBy(em => em.Episode.EpisodeNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<int> maybeId = await dbContext.ShowMetadata
                .Where(s => s.Title == metadata.Title && s.Year == metadata.Year)
                .Where(s => s.Show.LibraryPathId == libraryPathId)
                .SingleOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.ShowId);

            return await maybeId.Match(
                id =>
                {
                    return dbContext.Shows
                        .AsNoTracking()
                        .Include(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Artwork)
                        .Include(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Genres)
                        .Include(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Tags)
                        .Include(s => s.LibraryPath)
                        .ThenInclude(lp => lp.Library)
                        .OrderBy(s => s.Id)
                        .SingleOrDefaultAsync(s => s.Id == id)
                        .Map(Optional);
                },
                () => Option<Show>.None.AsTask());
        }

        public async Task<Either<BaseError, MediaItemScanResult<Show>>> AddShow(
            int libraryPathId,
            string showFolder,
            ShowMetadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            try
            {
                metadata.DateAdded = DateTime.UtcNow;
                metadata.Genres ??= new List<Genre>();
                metadata.Tags ??= new List<Tag>();
                var show = new Show
                {
                    LibraryPathId = libraryPathId,
                    ShowMetadata = new List<ShowMetadata> { metadata },
                    Seasons = new List<Season>()
                };

                await dbContext.Shows.AddAsync(show);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(show).Reference(s => s.LibraryPath).LoadAsync();
                await dbContext.Entry(show.LibraryPath).Reference(lp => lp.Library).LoadAsync();

                return new MediaItemScanResult<Show>(show) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<Season> maybeExisting = await dbContext.Seasons
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .OrderBy(s => s.ShowId)
                .ThenBy(s => s.SeasonNumber)
                .SingleOrDefaultAsync(s => s.ShowId == show.Id && s.SeasonNumber == seasonNumber);

            return await maybeExisting.Match(
                season => Right<BaseError, Season>(season).AsTask(),
                () => AddSeason(dbContext, show, libraryPathId, seasonNumber));
        }

        public async Task<Either<BaseError, Episode>> GetOrAddEpisode(
            Season season,
            LibraryPath libraryPath,
            string path)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<Episode> maybeExisting = await dbContext.Episodes
                .Include(i => i.EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
                .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

            return await maybeExisting.Match(
                episode => Right<BaseError, Episode>(episode).AsTask(),
                () => AddEpisode(dbContext, season, libraryPath.Id, path));
        }

        public Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath) =>
            _dbConnection.QueryAsync<string>(
                @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN Episode E on MV.EpisodeId = E.Id
                INNER JOIN MediaItem MI on E.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
                new { LibraryPathId = libraryPath.Id });

        public async Task<Unit> DeleteByPath(LibraryPath libraryPath, string path)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT E.Id
                FROM Episode E
                INNER JOIN MediaItem MI on E.Id = MI.Id
                INNER JOIN MediaVersion MV on E.Id = MV.EpisodeId
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
                new { LibraryPathId = libraryPath.Id, Path = path });

            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            foreach (int episodeId in ids)
            {
                Episode episode = await dbContext.Episodes.FindAsync(episodeId);
                dbContext.Episodes.Remove(episode);
            }

            await dbContext.SaveChangesAsync();

            return Unit.Default;
        }

        public async Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            List<Season> seasons = await dbContext.Seasons
                .Filter(s => s.LibraryPathId == libraryPath.Id)
                .Filter(s => s.Episodes.Count == 0)
                .ToListAsync();
            dbContext.Seasons.RemoveRange(seasons);
            await dbContext.SaveChangesAsync();
            return Unit.Default;
        }

        public async Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            List<Show> shows = await dbContext.Shows
                .Filter(s => s.LibraryPathId == libraryPath.Id)
                .Filter(s => s.Seasons.Count == 0)
                .ToListAsync();
            var ids = shows.Map(s => s.Id).ToList();
            dbContext.Shows.RemoveRange(shows);
            await dbContext.SaveChangesAsync();
            return ids;
        }

        public async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> GetOrAddPlexShow(
            PlexLibrary library,
            PlexShow item)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<PlexShow> maybeExisting = await dbContext.PlexShows
                .AsNoTracking()
                .Include(i => i.ShowMetadata)
                .ThenInclude(sm => sm.Genres)
                .Include(i => i.ShowMetadata)
                .ThenInclude(sm => sm.Tags)
                .Include(i => i.ShowMetadata)
                .ThenInclude(sm => sm.Studios)
                .Include(i => i.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(i => i.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .OrderBy(i => i.Key)
                .SingleOrDefaultAsync(i => i.Key == item.Key);

            return await maybeExisting.Match(
                plexShow => Right<BaseError, MediaItemScanResult<PlexShow>>(
                    new MediaItemScanResult<PlexShow>(plexShow) { IsAdded = true }).AsTask(),
                async () => await AddPlexShow(dbContext, library, item));
        }

        public async Task<Either<BaseError, PlexSeason>> GetOrAddPlexSeason(PlexLibrary library, PlexSeason item)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<PlexSeason> maybeExisting = await dbContext.PlexSeasons
                .AsNoTracking()
                .Include(i => i.SeasonMetadata)
                .ThenInclude(mm => mm.Artwork)
                .OrderBy(i => i.Key)
                .SingleOrDefaultAsync(i => i.Key == item.Key);

            return await maybeExisting.Match(
                plexSeason => Right<BaseError, PlexSeason>(plexSeason).AsTask(),
                async () => await AddPlexSeason(dbContext, library, item));
        }

        public async Task<Either<BaseError, PlexEpisode>> GetOrAddPlexEpisode(PlexLibrary library, PlexEpisode item)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<PlexEpisode> maybeExisting = await dbContext.PlexEpisodes
                .AsNoTracking()
                .Include(i => i.EpisodeMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .OrderBy(i => i.Key)
                .SingleOrDefaultAsync(i => i.Key == item.Key);

            return await maybeExisting.Match(
                plexEpisode => Right<BaseError, PlexEpisode>(plexEpisode).AsTask(),
                async () => await AddPlexEpisode(dbContext, library, item));
        }

        public Task<Unit> RemoveMissingPlexSeasons(string showKey, List<string> seasonKeys) =>
            _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN Season s ON m.Id = s.Id
                INNER JOIN PlexSeason ps ON ps.Id = m.Id
                INNER JOIN PlexShow P on P.Id = s.ShowId
                WHERE P.Key = @ShowKey AND ps.Key not in @Keys)",
                new { ShowKey = showKey, Keys = seasonKeys }).ToUnit();

        public Task<Unit> RemoveMissingPlexEpisodes(string seasonKey, List<string> episodeKeys) =>
            _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN Episode e ON m.Id = e.Id
                INNER JOIN PlexEpisode pe ON pe.Id = m.Id
                INNER JOIN PlexSeason P on P.Id = e.SeasonId
                WHERE P.Key = @SeasonKey AND pe.Key not in @Keys)",
                new { SeasonKey = seasonKey, Keys = episodeKeys }).ToUnit();

        public async Task<Unit> SetEpisodeNumber(Episode episode, int episodeNumber)
        {
            episode.EpisodeNumber = episodeNumber;
            await _dbConnection.ExecuteAsync(
                @"UPDATE Episode SET EpisodeNumber = @EpisodeNumber WHERE Id = @Id",
                new { EpisodeNumber = episodeNumber, episode.Id });
            return Unit.Default;
        }

        public async Task<List<Episode>> GetShowItems(int showId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM Show
            INNER JOIN Season ON Season.ShowId = Show.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE Show.Id = @ShowId",
                new { ShowId = showId });

            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Episodes
                .AsNoTracking()
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .Filter(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        public async Task<List<Episode>> GetSeasonItems(int seasonId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Episodes
                .AsNoTracking()
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .Filter(e => e.SeasonId == seasonId)
                .ToListAsync();
        }

        public Task<bool> AddGenre(ShowMetadata metadata, Genre genre) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Genre (Name, ShowMetadataId) VALUES (@Name, @MetadataId)",
                new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddTag(ShowMetadata metadata, Tag tag) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Tag (Name, ShowMetadataId) VALUES (@Name, @MetadataId)",
                new { tag.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddStudio(ShowMetadata metadata, Studio studio) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Studio (Name, ShowMetadataId) VALUES (@Name, @MetadataId)",
                new { studio.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public async Task<List<int>> RemoveMissingPlexShows(PlexLibrary library, List<string> showKeys)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                WHERE lp.LibraryId = @LibraryId AND ps.Key not in @Keys",
                new { LibraryId = library.Id, Keys = showKeys }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                WHERE lp.LibraryId = @LibraryId AND ps.Key not in @Keys)",
                new { LibraryId = library.Id, Keys = showKeys });

            return ids;
        }

        private static async Task<Either<BaseError, Season>> AddSeason(
            TvContext dbContext,
            Show show,
            int libraryPathId,
            int seasonNumber)
        {
            try
            {
                var season = new Season
                {
                    LibraryPathId = libraryPathId,
                    ShowId = show.Id,
                    SeasonNumber = seasonNumber,
                    Episodes = new List<Episode>(),
                    SeasonMetadata = new List<SeasonMetadata>
                    {
                        new()
                        {
                            DateAdded = DateTime.UtcNow
                        }
                    }
                };
                await dbContext.Seasons.AddAsync(season);
                await dbContext.SaveChangesAsync();
                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static async Task<Either<BaseError, Episode>> AddEpisode(
            TvContext dbContext,
            Season season,
            int libraryPathId,
            string path)
        {
            try
            {
                if (dbContext.MediaFiles.Any(mf => mf.Path == path))
                {
                    return BaseError.New("Multi-episode files are not yet supported");
                }

                var episode = new Episode
                {
                    LibraryPathId = libraryPathId,
                    SeasonId = season.Id,
                    EpisodeMetadata = new List<EpisodeMetadata>
                    {
                        new()
                        {
                            DateAdded = DateTime.UtcNow,
                            DateUpdated = DateTime.MinValue,
                            MetadataKind = MetadataKind.Fallback
                        }
                    },
                    MediaVersions = new List<MediaVersion>
                    {
                        new()
                        {
                            MediaFiles = new List<MediaFile>
                            {
                                new() { Path = path }
                            }
                        }
                    }
                };
                await dbContext.Episodes.AddAsync(episode);
                await dbContext.SaveChangesAsync();
                return episode;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> AddPlexShow(
            TvContext dbContext,
            PlexLibrary library,
            PlexShow item)
        {
            try
            {
                item.LibraryPathId = library.Paths.Head().Id;

                await dbContext.PlexShows.AddAsync(item);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
                await dbContext.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
                return new MediaItemScanResult<PlexShow>(item) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static async Task<Either<BaseError, PlexSeason>> AddPlexSeason(
            TvContext dbContext,
            PlexLibrary library,
            PlexSeason item)
        {
            try
            {
                item.LibraryPathId = library.Paths.Head().Id;

                await dbContext.PlexSeasons.AddAsync(item);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
                return item;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static async Task<Either<BaseError, PlexEpisode>> AddPlexEpisode(
            TvContext dbContext,
            PlexLibrary library,
            PlexEpisode item)
        {
            try
            {
                if (dbContext.MediaFiles.Any(mf => mf.Path == item.MediaVersions.Head().MediaFiles.Head().Path))
                {
                    return BaseError.New("Multi-episode files are not yet supported");
                }

                item.LibraryPathId = library.Paths.Head().Id;

                await dbContext.PlexEpisodes.AddAsync(item);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
                return item;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
