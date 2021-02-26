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
                .ToListAsync();

        public Task<Option<Show>> GetShow(int televisionShowId) =>
            _dbContext.Shows
                .AsNoTracking()
                .Filter(s => s.Id == televisionShowId)
                .Include(s => s.ShowMetadata)
                .SingleOrDefaultAsync()
                .Map(Optional);

        public Task<int> GetShowCount() =>
            _dbContext.Shows
                .AsNoTracking()
                .CountAsync();

        public Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize) =>
            // TODO: fix this
            new List<ShowMetadata>().AsTask();
        // _dbContext.ShowMetadata
        //     .AsNoTracking()
        //     .Include(s => s.Show)
        //     .OrderBy(s => s.Metadata == null ? string.Empty : s.Metadata.SortTitle)
        //     .Skip((pageNumber - 1) * pageSize)
        //     .Take(pageSize)
        //     .ToListAsync();

        public Task<List<Season>> GetAllSeasons() =>
            _dbContext.Seasons
                .AsNoTracking()
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ToListAsync();

        public Task<Option<Season>> GetSeason(int televisionSeasonId) =>
            _dbContext.Seasons
                .AsNoTracking()
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .SingleOrDefaultAsync(s => s.Id == televisionSeasonId)
                .Map(Optional);

        public Task<int> GetSeasonCount(int televisionShowId) =>
            _dbContext.Seasons
                .AsNoTracking()
                .Where(s => s.ShowId == televisionShowId)
                .CountAsync();

        public Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
            _dbContext.Seasons
                .AsNoTracking()
                .Where(s => s.ShowId == televisionShowId)
                .Include(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .OrderBy(s => s.SeasonNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public Task<Option<Episode>> GetEpisode(int televisionEpisodeId) =>
            _dbContext.Episodes
                .AsNoTracking()
                .Include(e => e.Season)
                .Include(e => e.EpisodeMetadata)
                .SingleOrDefaultAsync(s => s.Id == televisionEpisodeId)
                .Map(Optional);

        public Task<int> GetEpisodeCount(int televisionSeasonId) =>
            _dbContext.Episodes
                .AsNoTracking()
                .Where(e => e.SeasonId == televisionSeasonId)
                .CountAsync();

        public Task<List<EpisodeMetadata>> GetPagedEpisodes(
            int televisionSeasonId,
            int pageNumber,
            int pageSize) =>
            // TODO: fix this
            new List<EpisodeMetadata>().AsTask();
        // _dbContext.EpisodeMetadata
        //     .AsNoTracking()
        //     .Include(e => e.Metadata)
        //     .Include(e => e.Season)
        //     .ThenInclude(s => s.TelevisionShow)
        //     .ThenInclude(s => s.Metadata)
        //     .Where(e => e.SeasonId == televisionSeasonId)
        //     .OrderBy(s => s.Metadata.Episode)
        //     .Skip((pageNumber - 1) * pageSize)
        //     .Take(pageSize)
        //     .ToListAsync();

        public async Task<Option<Show>> GetShowByMetadata(ShowMetadata metadata)
        {
            Option<int> maybeId = await _dbContext.ShowMetadata
                .Where(s => s.Title == metadata.Title && s.ReleaseDate == metadata.ReleaseDate)
                .SingleOrDefaultAsync()
                .Map(sm => sm.ShowId)
                .Map(Optional);

            return await maybeId.Match(
                id =>
                {
                    return _dbContext.Shows
                        .Include(s => s.ShowMetadata)
                        .Filter(s => s.Id == id)
                        .SingleOrDefaultAsync()
                        .Map(Optional);
                },
                () => Option<Show>.None.AsTask());
        }

        public async Task<Either<BaseError, Show>> AddShow(
            int localMediaSourceId,
            string showFolder,
            ShowMetadata metadata)
        {
            try
            {
                var show = new Show
                {
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

        public async Task<Either<BaseError, Season>> GetOrAddSeason(
            Show show,
            string path,
            int seasonNumber)
        {
            Option<Season> maybeExisting = await _dbContext.Seasons
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                season => Right<BaseError, Season>(season).AsTask(),
                () => AddSeason(show, path, seasonNumber));
        }

        // TODO: don't use mediaSourceId, use library path id
        public async Task<Either<BaseError, Episode>> GetOrAddEpisode(
            Season season,
            LibraryPath libraryPath,
            string path)
        {
            Option<Episode> maybeExisting = await _dbContext.Episodes
                .Include(i => i.EpisodeMetadata)
                .Include(i => i.Statistics)
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                episode => Right<BaseError, Episode>(episode).AsTask(),
                () => AddEpisode(season, libraryPath.Id, path));
        }

        public Task<Unit> DeleteEmptyShows() =>
            _dbContext.Shows
                .Where(s => s.Seasons.Count == 0)
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
                .Filter(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        public Task<List<Episode>> GetSeasonItems(int seasonId) =>
            _dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Filter(e => e.SeasonId == seasonId)
                .ToListAsync();

        private async Task<Either<BaseError, Season>> AddSeason(
            Show show,
            string path,
            int seasonNumber)
        {
            try
            {
                var season = new Season
                {
                    ShowId = show.Id, Path = path, SeasonNumber = seasonNumber,
                    Episodes = new List<Episode>()
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

        private async Task<Either<BaseError, Episode>> AddEpisode(
            Season season,
            int mediaSourceId,
            string path)
        {
            try
            {
                var episode = new Episode
                {
                    LibraryPathId = mediaSourceId,
                    SeasonId = season.Id,
                    Path = path
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
