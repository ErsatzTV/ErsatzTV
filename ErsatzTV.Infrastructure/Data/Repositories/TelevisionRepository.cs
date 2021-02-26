using System;
using System.Collections.Generic;
using System.Data;
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

        public async Task<List<Episode>> GetShowItems(int televisionShowId) =>
            // TODO: fix this
            new();

        //             var parameters = new { ShowId = televisionShowId };
        //             return await _dbConnection
        //                 .QueryAsync<TelevisionEpisodeMediaItem, MediaItemStatistics, TelevisionEpisodeMetadata,
        //                     TelevisionEpisodeMediaItem>(
        //                     @"select tmi.Id, tmi.SeasonId, mi.LibraryPathId, mi.LastWriteTime, mi.Path, mi.Poster, mi.PosterLastWriteTime,
        // mi.Statistics_AudioCodec as AudioCodec, mi.Statistics_DisplayAspectRatio as DisplayAspectRatio, mi.Statistics_Duration as Duration, mi.Statistics_Height as Height, mi.Statistics_LastWriteTime as LastWriteTime, mi.Statistics_SampleAspectRatio as SampleAspectRatio,
        // mi.Statistics_VideoCodec as VideoCodec, mi.Statistics_VideoScanType as VideoScanType, mi.Statistics_Width as Width,
        // tem.TelevisionEpisodeId, tem.Id, tem.Season, tem.Episode, tem.Plot, tem.Aired, tem.Source, tem.LastWriteTime, tem.Title, tem.SortTitle
        // from TelevisionEpisode tmi
        // inner join MediaItem mi on tmi.Id = mi.Id
        // inner join TelevisionEpisodeMetadata tem on tem.TelevisionEpisodeId = tmi.Id
        // inner join TelevisionSeason tsn on tsn.Id = tmi.SeasonId
        // inner join TelevisionShow ts on ts.Id = tsn.TelevisionShowId
        // where ts.Id = @ShowId",
        //                     (episode, statistics, metadata) =>
        //                     {
        //                         episode.Statistics = statistics;
        //                         episode.Metadata = metadata;
        //                         return episode;
        //                     },
        //                     parameters,
        //                     splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
        public async Task<List<Episode>> GetSeasonItems(int televisionSeasonId) =>
            // TODO: fix this
            new();

        //             var parameters = new { SeasonId = televisionSeasonId };
        //             return await _dbConnection
        //                 .QueryAsync<TelevisionEpisodeMediaItem, MediaItemStatistics, TelevisionEpisodeMetadata,
        //                     TelevisionEpisodeMediaItem>(
        //                     @"select tmi.Id, tmi.SeasonId, mi.LibraryPathId, mi.LastWriteTime, mi.Path, mi.Poster, mi.PosterLastWriteTime,
        // mi.Statistics_AudioCodec as AudioCodec, mi.Statistics_DisplayAspectRatio as DisplayAspectRatio, mi.Statistics_Duration as Duration, mi.Statistics_Height as Height, mi.Statistics_LastWriteTime as LastWriteTime, mi.Statistics_SampleAspectRatio as SampleAspectRatio,
        // mi.Statistics_VideoCodec as VideoCodec, mi.Statistics_VideoScanType as VideoScanType, mi.Statistics_Width as Width,
        // tem.TelevisionEpisodeId, tem.Id, tem.Season, tem.Episode, tem.Plot, tem.Aired, tem.Source, tem.LastWriteTime, tem.Title, tem.SortTitle
        // from TelevisionEpisode tmi
        // inner join MediaItem mi on tmi.Id = mi.Id
        // inner join TelevisionEpisodeMetadata tem on tem.TelevisionEpisodeId = tmi.Id
        // inner join TelevisionSeason tsn on tsn.Id = tmi.SeasonId
        // where tsn.Id = @SeasonId",
        //                     (episode, statistics, metadata) =>
        //                     {
        //                         episode.Statistics = statistics;
        //                         episode.Metadata = metadata;
        //                         return episode;
        //                     },
        //                     parameters,
        //                     splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
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
