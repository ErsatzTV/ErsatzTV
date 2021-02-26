using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaCollectionRepository : IMediaCollectionRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly TvContext _dbContext;

        public MediaCollectionRepository(TvContext dbContext, IDbConnection dbConnection)
        {
            _dbContext = dbContext;
            _dbConnection = dbConnection;
        }

        public async Task<Collection> Add(Collection collection)
        {
            await _dbContext.Collections.AddAsync(collection);
            await _dbContext.SaveChangesAsync();
            return collection;
        }

        public Task<Option<Collection>> Get(int id) =>
            _dbContext.Collections.SingleOrDefaultAsync(c => c.Id == id).Map(Optional);

        public Task<Option<Collection>> GetCollectionWithItems(int id) =>
            _dbContext.Collections
                .Include(c => c.MediaItems)
                .ThenInclude(i => i.LibraryPath)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Movie).MovieMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Show).ShowMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Season).Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).EpisodeMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<Option<Collection>> GetCollectionWithItemsUntracked(int id) =>
            _dbContext.Collections
                .AsNoTracking()
                .Include(c => c.MediaItems)
                .ThenInclude(i => i.LibraryPath)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Movie).MovieMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Show).ShowMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Season).Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).EpisodeMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<List<Collection>> GetAll() =>
            _dbContext.Collections.ToListAsync();

        public Task<Option<List<MediaItem>>> GetItems(int id) =>
            Get(id).MapT(c => c.MediaItems);

        public Task Update(Collection collection)
        {
            _dbContext.Collections.Update(collection);
            return _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int collectionId)
        {
            Collection mediaCollection = await _dbContext.Collections.FindAsync(collectionId);
            _dbContext.Collections.Remove(mediaCollection);
            await _dbContext.SaveChangesAsync();
        }

//         private async Task<List<TelevisionEpisodeMediaItem>> GetTelevisionShowItems(SimpleMediaCollection collection)
//         {
//             var parameters = new { CollectionId = collection.Id };
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
// inner join SimpleMediaCollectionShow s on s.TelevisionShowsId = ts.Id
// where s.SimpleMediaCollectionsId = @CollectionId",
//                     (episode, statistics, metadata) =>
//                     {
//                         episode.Statistics = statistics;
//                         episode.Metadata = metadata;
//                         return episode;
//                     },
//                     parameters,
//                     splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
//         }
//
//         private async Task<List<TelevisionEpisodeMediaItem>> GetTelevisionSeasonItems(SimpleMediaCollection collection)
//         {
//             var parameters = new { CollectionId = collection.Id };
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
// inner join SimpleMediaCollectionSeason s on s.TelevisionSeasonsId = tsn.Id
// where s.SimpleMediaCollectionsId = @CollectionId",
//                     (episode, statistics, metadata) =>
//                     {
//                         episode.Statistics = statistics;
//                         episode.Metadata = metadata;
//                         return episode;
//                     },
//                     parameters,
//                     splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
//         }
//
//         private async Task<List<TelevisionEpisodeMediaItem>> GetTelevisionEpisodeItems(SimpleMediaCollection collection)
//         {
//             var parameters = new { CollectionId = collection.Id };
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
// inner join SimpleMediaCollectionEpisode s on s.TelevisionEpisodesId = tmi.Id
// where s.SimpleMediaCollectionsId = @CollectionId",
//                     (episode, statistics, metadata) =>
//                     {
//                         episode.Statistics = statistics;
//                         episode.Metadata = metadata;
//                         return episode;
//                     },
//                     parameters,
//                     splitOn: "AudioCodec,TelevisionEpisodeId").Map(result => result.ToList());
//         }
    }
}
