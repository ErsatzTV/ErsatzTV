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
        private readonly TvContext _dbContext;
        private readonly IDbConnection _dbConnection;

        public MediaCollectionRepository(TvContext dbContext, IDbConnection dbConnection)
        {
            _dbContext = dbContext;
            _dbConnection = dbConnection;
        }

        public async Task<SimpleMediaCollection> Add(SimpleMediaCollection collection)
        {
            await _dbContext.SimpleMediaCollections.AddAsync(collection);
            await _dbContext.SaveChangesAsync();
            return collection;
        }

        public Task<Option<MediaCollection>> Get(int id) =>
            _dbContext.MediaCollections.SingleOrDefaultAsync(c => c.Id == id).Map(Optional);

        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollection(int id) =>
            _dbContext.SimpleMediaCollections
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollectionWithItems(int id) =>
            _dbContext.SimpleMediaCollections
                .Include(s => s.Movies)
                .ThenInclude(m => m.Source)
                .Include(s => s.TelevisionShows)
                .ThenInclude(s => s.Metadata)
                .Include(s => s.TelevisionSeasons)
                .Include(s => s.TelevisionEpisodes)
                .ThenInclude(s => s.Metadata)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollectionWithItemsUntracked(int id) =>
            _dbContext.SimpleMediaCollections
                .AsNoTracking()
                .Include(s => s.Movies)
                .ThenInclude(i => i.Source)
                .Include(s => s.Movies)
                .ThenInclude(m => m.Metadata)
                .Include(s => s.TelevisionShows)
                .ThenInclude(s => s.Metadata)
                .Include(s => s.TelevisionSeasons)
                .ThenInclude(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .Include(s => s.TelevisionEpisodes)
                .ThenInclude(s => s.Metadata)
                .Include(s => s.TelevisionEpisodes)
                .ThenInclude(e => e.Season)
                .ThenInclude(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<List<SimpleMediaCollection>> GetSimpleMediaCollections() =>
            _dbContext.SimpleMediaCollections.ToListAsync();

        public Task<List<MediaCollection>> GetAll() =>
            _dbContext.MediaCollections.ToListAsync();

        public Task<Option<List<MediaItem>>> GetItems(int id) =>
            Get(id).MapT(
                collection => collection switch
                {
                    SimpleMediaCollection s => SimpleItems(s),
                    _ => throw new NotSupportedException($"Unsupported collection type {collection.GetType().Name}")
                }).Bind(x => x.Sequence());

        public Task<Option<List<MediaItem>>> GetSimpleMediaCollectionItems(int id) =>
            GetSimpleMediaCollection(id).MapT(SimpleItems).Bind(x => x.Sequence());

        public Task Update(SimpleMediaCollection collection)
        {
            _dbContext.SimpleMediaCollections.Update(collection);
            return _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int mediaCollectionId)
        {
            MediaCollection mediaCollection = await _dbContext.MediaCollections.FindAsync(mediaCollectionId);
            _dbContext.MediaCollections.Remove(mediaCollection);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<List<MediaItem>> SimpleItems(SimpleMediaCollection collection)
        {
            var result = new List<MediaItem>();

            await _dbContext.Entry(collection).Collection(c => c.Movies).LoadAsync();
            result.AddRange(collection.Movies);

            result.AddRange(await GetTelevisionShowItems(collection));
            result.AddRange(await GetTelevisionSeasonItems(collection));
            result.AddRange(await GetTelevisionEpisodeItems(collection));

            return result.Distinct().ToList();
        }

        private async Task<List<TelevisionEpisodeMediaItem>> GetTelevisionShowItems(SimpleMediaCollection collection)
        {
            var parameters = new { CollectionId = collection.Id };
            IEnumerable<int> showItemIds = await _dbConnection.QueryAsync<int>(
                @"select tmi.Id
from TelevisionEpisodes tmi
inner join TelevisionSeasons tsn on tsn.Id = tmi.SeasonId
inner join TelevisionShows ts on ts.Id = tsn.TelevisionShowId
inner join SimpleMediaCollectionShows s on s.TelevisionShowsId = ts.Id
where s.SimpleMediaCollectionsId = @CollectionId",
                parameters);

            return await _dbContext.TelevisionEpisodeMediaItems
                .AsNoTracking()
                .Include(e => e.Metadata)
                .Where(mi => showItemIds.Contains(mi.Id))
                .ToListAsync();
        }

        private async Task<List<TelevisionEpisodeMediaItem>> GetTelevisionSeasonItems(SimpleMediaCollection collection)
        {
            var parameters = new { CollectionId = collection.Id };
            IEnumerable<int> seasonItemIds = await _dbConnection.QueryAsync<int>(
                @"select tmi.Id
from TelevisionEpisodes tmi
inner join TelevisionSeasons tsn on tsn.Id = tmi.SeasonId
inner join SimpleMediaCollectionSeasons s on s.TelevisionSeasonsId = tsn.Id
where s.SimpleMediaCollectionsId = @CollectionId",
                parameters);

            return await _dbContext.TelevisionEpisodeMediaItems
                .AsNoTracking()
                .Include(e => e.Metadata)
                .Where(mi => seasonItemIds.Contains(mi.Id))
                .ToListAsync();
        }

        private async Task<List<TelevisionEpisodeMediaItem>> GetTelevisionEpisodeItems(SimpleMediaCollection collection)
        {
            var parameters = new { CollectionId = collection.Id };
            IEnumerable<int> episodeItemIds = await _dbConnection.QueryAsync<int>(
                    @"select s.TelevisionEpisodesId as Id
from SimpleMediaCollectionEpisodes s
where s.SimpleMediaCollectionsId = @CollectionId",
                    parameters);

            return await _dbContext.TelevisionEpisodeMediaItems
                .AsNoTracking()
                .Include(e => e.Metadata)
                .Where(mi => episodeItemIds.Contains(mi.Id))
                .ToListAsync();
        }
    }
}
