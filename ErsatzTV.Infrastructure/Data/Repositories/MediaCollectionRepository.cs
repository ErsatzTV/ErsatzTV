using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public MediaCollectionRepository(TvContext dbContext) => _dbContext = dbContext;

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
                .ThenInclude(i => i.Source)
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
                .Include(s => s.TelevisionShows)
                .ThenInclude(s => s.Metadata)
                .Include(s => s.TelevisionSeasons)
                .ThenInclude(s => s.TelevisionShow)
                .ThenInclude(s => s.Metadata)
                .Include(s => s.TelevisionEpisodes)
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

        private async Task<List<MediaItem>> GetTelevisionShowItems(SimpleMediaCollection collection)
        {
            // TODO: would be nice to get the media items in one go, but ef...
            List<int> showItemIds = await _dbContext.GenericIntegerIds.FromSqlRaw(
                    @"select tmi.Id
from TelevisionEpisodes tmi
inner join TelevisionSeasons tsn on tsn.Id = tmi.SeasonId
inner join TelevisionShows ts on ts.Id = tsn.TelevisionShowId
inner join SimpleMediaCollectionShows s on s.TelevisionShowsId = ts.Id
where s.SimpleMediaCollectionsId = {0}",
                    collection.Id)
                .Select(i => i.Id)
                .ToListAsync();

            return await _dbContext.MediaItems
                .AsNoTracking()
                .Where(mi => showItemIds.Contains(mi.Id))
                .ToListAsync();
        }

        private async Task<List<MediaItem>> GetTelevisionSeasonItems(SimpleMediaCollection collection)
        {
            // TODO: would be nice to get the media items in one go, but ef...
            List<int> seasonItemIds = await _dbContext.GenericIntegerIds.FromSqlRaw(
                    @"select tmi.Id
from TelevisionEpisodes tmi
inner join TelevisionSeasons tsn on tsn.Id = tmi.SeasonId
inner join SimpleMediaCollectionSeasons s on s.TelevisionSeasonsId = tsn.Id
where s.SimpleMediaCollectionsId = {0}",
                    collection.Id)
                .Select(i => i.Id)
                .ToListAsync();

            return await _dbContext.MediaItems
                .AsNoTracking()
                .Where(mi => seasonItemIds.Contains(mi.Id))
                .ToListAsync();
        }

        private async Task<List<MediaItem>> GetTelevisionEpisodeItems(SimpleMediaCollection collection)
        {
            // TODO: would be nice to get the media items in one go, but ef...
            List<int> episodeItemIds = await _dbContext.GenericIntegerIds.FromSqlRaw(
                    @"select s.TelevisionEpisodesId as Id
from SimpleMediaCollectionEpisodes s
where s.SimpleMediaCollectionsId = {0}",
                    collection.Id)
                .Select(i => i.Id)
                .ToListAsync();

            return await _dbContext.MediaItems
                .AsNoTracking()
                .Where(mi => episodeItemIds.Contains(mi.Id))
                .ToListAsync();
        }
    }
}
