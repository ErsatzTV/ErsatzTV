using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class TelevisionRepository : ITelevisionRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly TvContext _dbContext;

        public TelevisionRepository(TvContext dbContext, IDbConnection dbConnection)
        {
            _dbContext = dbContext;
            _dbConnection = dbConnection;
        }

        public Task<bool> AllShowsExist(List<int> showIds) =>
            _dbConnection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM Show WHERE Id in @ShowIds",
                    new { ShowIds = showIds })
                .Map(c => c == showIds.Count);

        public async Task<bool> Update(Show show)
        {
            _dbContext.Shows.Update(show);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Season season)
        {
            _dbContext.Seasons.Update(season);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Episode episode)
        {
            _dbContext.Episodes.Update(episode);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public Task<List<Show>> GetAllShows() =>
            _dbContext.Shows
                .AsNoTracking()
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .ToListAsync();

        public Task<Option<Show>> GetShow(int showId) =>
            _dbContext.Shows
                .AsNoTracking()
                .Filter(s => s.Id == showId)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Genres)
                .Include(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Tags)
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync()
                .Map(Optional);

        public Task<int> GetShowCount() =>
            _dbContext.ShowMetadata
                .AsNoTracking()
                .GroupBy(sm => new { sm.Title, sm.Year })
                .CountAsync();

        public Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize) =>
            _dbContext.ShowMetadata.FromSqlRaw(
                    @"SELECT * FROM ShowMetadata WHERE Id IN
            (SELECT MIN(Id) FROM ShowMetadata GROUP BY Title, Year, MetadataKind HAVING MetadataKind = MAX(MetadataKind))
            ORDER BY SortTitle
            LIMIT {0} OFFSET {1}",
                    pageSize,
                    (pageNumber - 1) * pageSize)
                .Include(mm => mm.Artwork)
                .OrderBy(mm => mm.SortTitle)
                .ToListAsync();

        public Task<List<Season>> GetAllSeasons() =>
            _dbContext.Seasons
                .AsNoTracking()
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .ToListAsync();

        public Task<Option<Season>> GetSeason(int seasonId) =>
            _dbContext.Seasons
                .AsNoTracking()
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync(s => s.Id == seasonId)
                .Map(Optional);

        public Task<int> GetSeasonCount(int showId) =>
            _dbContext.Seasons
                .AsNoTracking()
                .CountAsync(s => s.ShowId == showId);

        public async Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize)
        {
            List<int> showIds = await _dbConnection.QueryAsync<int>(
                    @"SELECT m1.ShowId
                FROM ShowMetadata m1
                LEFT OUTER JOIN ShowMetadata m2 ON m2.ShowId = @ShowId
                WHERE m1.Title = m2.Title AND m1.Year = m2.Year",
                    new { ShowId = televisionShowId })
                .Map(results => results.ToList());

            return await _dbContext.Seasons
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

        public Task<Option<Episode>> GetEpisode(int episodeId) =>
            _dbContext.Episodes
                .AsNoTracking()
                .Include(e => e.Season)
                .Include(e => e.EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync(s => s.Id == episodeId)
                .Map(Optional);

        public Task<int> GetEpisodeCount(int seasonId) =>
            _dbContext.Episodes
                .AsNoTracking()
                .CountAsync(e => e.SeasonId == seasonId);

        public Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize) =>
            _dbContext.EpisodeMetadata
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

        public async Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata)
        {
            Option<int> maybeId = await _dbContext.ShowMetadata
                .Where(s => s.Title == metadata.Title && s.Year == metadata.Year)
                .Where(s => s.Show.LibraryPathId == libraryPathId)
                .SingleOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.ShowId);

            return await maybeId.Match(
                id =>
                {
                    return _dbContext.Shows
                        .Include(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Artwork)
                        .Include(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Genres)
                        .Include(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Tags)
                        .OrderBy(s => s.Id)
                        .SingleOrDefaultAsync(s => s.Id == id)
                        .Map(Optional);
                },
                () => Option<Show>.None.AsTask());
        }

        public async Task<Either<BaseError, Show>> AddShow(int libraryPathId, string showFolder, ShowMetadata metadata)
        {
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

                await _dbContext.Shows.AddAsync(show);
                await _dbContext.SaveChangesAsync();

                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber)
        {
            Option<Season> maybeExisting = await _dbContext.Seasons
                .Include(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .SingleOrDefaultAsync(s => s.ShowId == show.Id && s.SeasonNumber == seasonNumber);

            return await maybeExisting.Match(
                season => Right<BaseError, Season>(season).AsTask(),
                () => AddSeason(show, libraryPathId, seasonNumber));
        }

        public async Task<Either<BaseError, Episode>> GetOrAddEpisode(
            Season season,
            LibraryPath libraryPath,
            string path)
        {
            Option<Episode> maybeExisting = await _dbContext.Episodes
                .Include(i => i.EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
                .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

            return await maybeExisting.Match(
                episode => Right<BaseError, Episode>(episode).AsTask(),
                () => AddEpisode(season, libraryPath.Id, path));
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

            foreach (int episodeId in ids)
            {
                Episode episode = await _dbContext.Episodes.FindAsync(episodeId);
                _dbContext.Episodes.Remove(episode);
            }

            await _dbContext.SaveChangesAsync();

            return Unit.Default;
        }

        public Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath) =>
            _dbContext.Seasons
                .Filter(s => s.LibraryPathId == libraryPath.Id)
                .Filter(s => s.Episodes.Count == 0)
                .ToListAsync()
                .Bind(
                    list =>
                    {
                        _dbContext.Seasons.RemoveRange(list);
                        return _dbContext.SaveChangesAsync();
                    })
                .ToUnit();

        public Task<Unit> DeleteEmptyShows(LibraryPath libraryPath) =>
            _dbContext.Shows
                .Filter(s => s.LibraryPathId == libraryPath.Id)
                .Filter(s => s.Seasons.Count == 0)
                .ToListAsync()
                .Bind(
                    list =>
                    {
                        _dbContext.Shows.RemoveRange(list);
                        return _dbContext.SaveChangesAsync();
                    })
                .ToUnit();

        public async Task<List<Episode>> GetShowItems(int showId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM Show
            INNER JOIN Season ON Season.ShowId = Show.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE Show.Id = @ShowId",
                new { ShowId = showId });

            return await _dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .Filter(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        public Task<List<Episode>> GetSeasonItems(int seasonId) =>
            _dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .Filter(e => e.SeasonId == seasonId)
                .ToListAsync();

        private async Task<Either<BaseError, Season>> AddSeason(Show show, int libraryPathId, int seasonNumber)
        {
            try
            {
                var season = new Season
                {
                    LibraryPathId = libraryPathId,
                    ShowId = show.Id,
                    SeasonNumber = seasonNumber,
                    Episodes = new List<Episode>(),
                    SeasonMetadata = new List<SeasonMetadata>()
                };
                await _dbContext.Seasons.AddAsync(season);
                await _dbContext.SaveChangesAsync();
                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, Episode>> AddEpisode(Season season, int libraryPathId, string path)
        {
            try
            {
                var episode = new Episode
                {
                    LibraryPathId = libraryPathId,
                    SeasonId = season.Id,
                    EpisodeMetadata = new List<EpisodeMetadata>(),
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
                await _dbContext.Episodes.AddAsync(episode);
                await _dbContext.SaveChangesAsync();
                return episode;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
