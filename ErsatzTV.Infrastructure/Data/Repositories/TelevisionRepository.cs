using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly TvContext _dbContext;

        public TelevisionRepository(TvContext dbContext) => _dbContext = dbContext;

        public Task<List<TelevisionShow>> GetAllByMediaSourceId(int mediaSourceId) =>
            _dbContext.TelevisionShows
                .Include(s => s.Metadata)
                .Include(s => s.Seasons)
                .ThenInclude(s => s.Episodes)
                .ToListAsync();

        public async Task<bool> Update(TelevisionShow show)
        {
            _dbContext.TelevisionShows.Update(show);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(TelevisionSeason season)
        {
            _dbContext.TelevisionSeasons.Update(season);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(TelevisionEpisodeMediaItem episode)
        {
            _dbContext.TelevisionEpisodeMediaItems.Update(episode);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public Task<Option<TelevisionShow>> GetShow(int televisionShowId) =>
            _dbContext.TelevisionShows
                .AsNoTracking()
                .Filter(s => s.Id == televisionShowId)
                .Include(s => s.Metadata)
                .SingleOrDefaultAsync()
                .Map(Optional);

        // TODO: test with split folders (same show in different sources)
        public Task<int> GetShowCount() =>
            _dbContext.TelevisionShows
                .AsNoTracking()
                // .GroupBy(s => new { s.Metadata.Title, s.Metadata.Year })
                .CountAsync();

        // TODO: test with split folders (same show in different sources)
        public Task<List<TelevisionShow>> GetPagedShows(int pageNumber, int pageSize) =>
            _dbContext.TelevisionShows
                .Include(s => s.Metadata)
                .OrderBy(s => s.Metadata.SortTitle)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

        public Task<int> GetSeasonCount(int televisionShowId) =>
            _dbContext.TelevisionSeasons
                .AsNoTracking()
                .Where(s => s.TelevisionShowId == televisionShowId)
                .CountAsync();

        public Task<List<TelevisionSeason>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
            _dbContext.TelevisionSeasons
                .Where(s => s.TelevisionShowId == televisionShowId)
                .OrderBy(s => s.Number)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

        public async Task<Either<BaseError, TelevisionShow>> GetOrAddShow(int mediaSourceId, string path)
        {
            Option<TelevisionShow> maybeExisting = await _dbContext.TelevisionShows
                .Include(i => i.Metadata)
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                show => Right<BaseError, TelevisionShow>(show).AsTask(),
                () => AddShow(mediaSourceId, path));
        }

        public async Task<Either<BaseError, TelevisionSeason>> GetOrAddSeason(
            TelevisionShow show,
            string path,
            int seasonNumber)
        {
            Option<TelevisionSeason> maybeExisting = await _dbContext.TelevisionSeasons
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                season => Right<BaseError, TelevisionSeason>(season).AsTask(),
                () => AddSeason(show, path, seasonNumber));
        }

        public async Task<Either<BaseError, TelevisionEpisodeMediaItem>> GetOrAddEpisode(
            TelevisionSeason season,
            int mediaSourceId,
            string path)
        {
            Option<TelevisionEpisodeMediaItem> maybeExisting = await _dbContext.TelevisionEpisodeMediaItems
                .Include(i => i.Metadata)
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                episode => Right<BaseError, TelevisionEpisodeMediaItem>(episode).AsTask(),
                () => AddEpisode(season, mediaSourceId, path));
        }

        private async Task<Either<BaseError, TelevisionShow>> AddShow(int mediaSourceId, string path)
        {
            try
            {
                var show = new TelevisionShow
                    { MediaSourceId = mediaSourceId, Path = path, Seasons = new List<TelevisionSeason>() };
                await _dbContext.TelevisionShows.AddAsync(show);
                await _dbContext.SaveChangesAsync();
                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, TelevisionSeason>> AddSeason(
            TelevisionShow show,
            string path,
            int seasonNumber)
        {
            try
            {
                var season = new TelevisionSeason
                {
                    TelevisionShowId = show.Id, Path = path, Number = seasonNumber,
                    Episodes = new List<TelevisionEpisodeMediaItem>()
                };
                await _dbContext.TelevisionSeasons.AddAsync(season);
                await _dbContext.SaveChangesAsync();
                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, TelevisionEpisodeMediaItem>> AddEpisode(
            TelevisionSeason season,
            int mediaSourceId,
            string path)
        {
            try
            {
                var episode = new TelevisionEpisodeMediaItem
                {
                    MediaSourceId = mediaSourceId,
                    SeasonId = season.Id,
                    Path = path
                };
                await _dbContext.TelevisionEpisodeMediaItems.AddAsync(episode);
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
