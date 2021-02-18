using System;
using System.Collections.Generic;
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

        public Task<Unit> Add(TelevisionShow show) =>
            _dbContext.TelevisionShows.AddAsync(show).AsTask()
                .Bind(_ => _dbContext.SaveChangesAsync())
                .ToUnit();

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

        public async Task<bool> Update(TelevisionEpisodeMediaItem episode)
        {
            _dbContext.TelevisionEpisodeMediaItems.Update(episode);
            return await _dbContext.SaveChangesAsync() > 0;
        }

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
