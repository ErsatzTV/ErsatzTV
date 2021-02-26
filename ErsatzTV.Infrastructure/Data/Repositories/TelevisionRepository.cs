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

        public Task<List<TelevisionShow>> GetAllShows() =>
            _dbContext.TelevisionShows
                .AsNoTracking()
                .Include(s => s.Metadata)
                .ToListAsync();

        public Task<Option<TelevisionShow>> GetShow(int televisionShowId) =>
            _dbContext.TelevisionShows
                .AsNoTracking()
                .Filter(s => s.Id == televisionShowId)
                .Include(s => s.Metadata)
                .SingleOrDefaultAsync()
                .Map(Optional);

        public Task<int> GetShowCount() =>
            _dbContext.TelevisionShows
                .AsNoTracking()
                .CountAsync();

        public Task<List<TelevisionShow>> GetPagedShows(int pageNumber, int pageSize) =>
            _dbContext.TelevisionShows
                .AsNoTracking()
                .Include(s => s.Metadata)
                .OrderBy(s => s.Metadata == null ? string.Empty : s.Metadata.SortTitle)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public Task<List<TelevisionSeason>> GetAllSeasons() =>
            _dbContext.TelevisionSeasons
                .AsNoTracking()
                .Include(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .ToListAsync();

        public Task<Option<TelevisionSeason>> GetSeason(int televisionSeasonId) =>
            _dbContext.TelevisionSeasons
                .AsNoTracking()
                .Include(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .SingleOrDefaultAsync(s => s.Id == televisionSeasonId)
                .Map(Optional);

        public Task<int> GetSeasonCount(int televisionShowId) =>
            _dbContext.TelevisionSeasons
                .AsNoTracking()
                .Where(s => s.TelevisionShowId == televisionShowId)
                .CountAsync();

        public Task<List<TelevisionSeason>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
            _dbContext.TelevisionSeasons
                .AsNoTracking()
                .Where(s => s.TelevisionShowId == televisionShowId)
                .Include(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .OrderBy(s => s.Number)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public Task<Option<TelevisionEpisodeMediaItem>> GetEpisode(int televisionEpisodeId) =>
            _dbContext.TelevisionEpisodeMediaItems
                .AsNoTracking()
                .Include(s => s.Season)
                .Include(s => s.Metadata)
                .SingleOrDefaultAsync(s => s.Id == televisionEpisodeId)
                .Map(Optional);

        public Task<int> GetEpisodeCount(int televisionSeasonId) =>
            _dbContext.TelevisionEpisodeMediaItems
                .AsNoTracking()
                .Where(e => e.SeasonId == televisionSeasonId)
                .CountAsync();

        public Task<List<TelevisionEpisodeMediaItem>> GetPagedEpisodes(
            int televisionSeasonId,
            int pageNumber,
            int pageSize) =>
            _dbContext.TelevisionEpisodeMediaItems
                .AsNoTracking()
                .Include(e => e.Metadata)
                .Include(e => e.Season)
                .ThenInclude(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .Where(e => e.SeasonId == televisionSeasonId)
                .OrderBy(s => s.Metadata.Episode)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        // TODO: lookup by library path id, not media source id
        public async Task<Option<TelevisionShow>> GetShowByPath(int mediaSourceId, string path)
        {
            Option<int> maybeShowId = await _dbContext.LocalTelevisionShowSources
                .SingleOrDefaultAsync(s => s.MediaSourceId == mediaSourceId && s.Path == path)
                .Map(Optional)
                .MapT(s => s.TelevisionShowId);

            return await maybeShowId.Match<Task<Option<TelevisionShow>>>(
                async id => await _dbContext.TelevisionShows
                    .Include(s => s.Metadata)
                    .Include(s => s.Sources)
                    .SingleOrDefaultAsync(s => s.Id == id),
                () => Task.FromResult(Option<TelevisionShow>.None));
        }

        public async Task<Option<TelevisionShow>> GetShowByMetadata(TelevisionShowMetadata metadata)
        {
            Option<TelevisionShow> maybeShow = await _dbContext.TelevisionShows
                .Include(s => s.Metadata)
                .Where(s => s.Metadata.Title == metadata.Title && s.Metadata.Year == metadata.Year)
                .SingleOrDefaultAsync()
                .Map(Optional);

            await maybeShow.IfSomeAsync(
                async show =>
                {
                    await _dbContext.Entry(show).Reference(s => s.Metadata).LoadAsync();
                    await _dbContext.Entry(show).Collection(s => s.Sources).LoadAsync();
                });

            return maybeShow;
        }

        public async Task<Either<BaseError, TelevisionShow>> AddShow(
            int localMediaSourceId,
            string showFolder,
            TelevisionShowMetadata metadata)
        {
            try
            {
                var show = new TelevisionShow
                {
                    Sources = new List<TelevisionShowSource>(),
                    Metadata = metadata,
                    Seasons = new List<TelevisionSeason>()
                };

                show.Sources.Add(
                    new LocalTelevisionShowSource
                    {
                        MediaSourceId = localMediaSourceId,
                        Path = showFolder,
                        TelevisionShow = show
                    });

                await _dbContext.TelevisionShows.AddAsync(show);
                await _dbContext.SaveChangesAsync();

                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
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

        // TODO: don't use mediaSourceId, use library path id
        public async Task<Either<BaseError, TelevisionEpisodeMediaItem>> GetOrAddEpisode(
            TelevisionSeason season,
            LibraryPath libraryPath,
            string path)
        {
            Option<TelevisionEpisodeMediaItem> maybeExisting = await _dbContext.TelevisionEpisodeMediaItems
                .Include(i => i.Metadata)
                .Include(i => i.Statistics)
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                episode => Right<BaseError, TelevisionEpisodeMediaItem>(episode).AsTask(),
                () => AddEpisode(season, libraryPath.Id, path));
        }

        public Task<Unit> DeleteMissingSources(int localMediaSourceId, List<string> allFolders) =>
            _dbContext.LocalTelevisionShowSources
                .Where(s => s.MediaSourceId == localMediaSourceId && !allFolders.Contains(s.Path))
                .ToListAsync()
                .Bind(
                    list =>
                    {
                        _dbContext.LocalTelevisionShowSources.RemoveRange(list);
                        return _dbContext.SaveChangesAsync();
                    })
                .ToUnit();

        public Task<Unit> DeleteEmptyShows() =>
            _dbContext.TelevisionShows
                .Where(s => s.Sources.Count == 0)
                .ToListAsync()
                .Bind(
                    list =>
                    {
                        _dbContext.TelevisionShows.RemoveRange(list);
                        return _dbContext.SaveChangesAsync();
                    })
                .ToUnit();

        public async Task<List<TelevisionEpisodeMediaItem>> GetShowItems(int televisionShowId)
        {
            var parameters = new { ShowId = televisionShowId };
            return await _dbConnection
                .QueryAsync<TelevisionEpisodeMediaItem, MediaItemStatistics, TelevisionEpisodeMetadata,
                    TelevisionEpisodeMediaItem>(
                    @"select tmi.Id, tmi.SeasonId, mi.LibraryPathId, mi.LastWriteTime, mi.Path, mi.Poster, mi.PosterLastWriteTime,
mi.Statistics_AudioCodec as AudioCodec, mi.Statistics_DisplayAspectRatio as DisplayAspectRatio, mi.Statistics_Duration as Duration, mi.Statistics_Height as Height, mi.Statistics_LastWriteTime as LastWriteTime, mi.Statistics_SampleAspectRatio as SampleAspectRatio,
mi.Statistics_VideoCodec as VideoCodec, mi.Statistics_VideoScanType as VideoScanType, mi.Statistics_Width as Width,
tem.TelevisionEpisodeId, tem.Id, tem.Season, tem.Episode, tem.Plot, tem.Aired, tem.Source, tem.LastWriteTime, tem.Title, tem.SortTitle
from TelevisionEpisode tmi
inner join MediaItem mi on tmi.Id = mi.Id
inner join TelevisionEpisodeMetadata tem on tem.TelevisionEpisodeId = tmi.Id
inner join TelevisionSeason tsn on tsn.Id = tmi.SeasonId
inner join TelevisionShow ts on ts.Id = tsn.TelevisionShowId
where ts.Id = @ShowId",
                    (episode, statistics, metadata) =>
                    {
                        episode.Statistics = statistics;
                        episode.Metadata = metadata;
                        return episode;
                    },
                    parameters,
                    splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
        }

        public async Task<List<TelevisionEpisodeMediaItem>> GetSeasonItems(int televisionSeasonId)
        {
            var parameters = new { SeasonId = televisionSeasonId };
            return await _dbConnection
                .QueryAsync<TelevisionEpisodeMediaItem, MediaItemStatistics, TelevisionEpisodeMetadata,
                    TelevisionEpisodeMediaItem>(
                    @"select tmi.Id, tmi.SeasonId, mi.LibraryPathId, mi.LastWriteTime, mi.Path, mi.Poster, mi.PosterLastWriteTime,
mi.Statistics_AudioCodec as AudioCodec, mi.Statistics_DisplayAspectRatio as DisplayAspectRatio, mi.Statistics_Duration as Duration, mi.Statistics_Height as Height, mi.Statistics_LastWriteTime as LastWriteTime, mi.Statistics_SampleAspectRatio as SampleAspectRatio,
mi.Statistics_VideoCodec as VideoCodec, mi.Statistics_VideoScanType as VideoScanType, mi.Statistics_Width as Width,
tem.TelevisionEpisodeId, tem.Id, tem.Season, tem.Episode, tem.Plot, tem.Aired, tem.Source, tem.LastWriteTime, tem.Title, tem.SortTitle
from TelevisionEpisode tmi
inner join MediaItem mi on tmi.Id = mi.Id
inner join TelevisionEpisodeMetadata tem on tem.TelevisionEpisodeId = tmi.Id
inner join TelevisionSeason tsn on tsn.Id = tmi.SeasonId
where tsn.Id = @SeasonId",
                    (episode, statistics, metadata) =>
                    {
                        episode.Statistics = statistics;
                        episode.Metadata = metadata;
                        return episode;
                    },
                    parameters,
                    splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
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
                    LibraryPathId = mediaSourceId,
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
