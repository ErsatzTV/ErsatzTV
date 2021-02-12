using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
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
            Get(id).Map(c => c.OfType<SimpleMediaCollection>().HeadOrNone());

        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollectionWithItems(int id) =>
            _dbContext.SimpleMediaCollections
                .Include(s => s.Items)
                .ThenInclude(i => i.Source)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<Option<TelevisionMediaCollection>> GetTelevisionMediaCollection(int id) =>
            Get(id).Map(c => c.OfType<TelevisionMediaCollection>().HeadOrNone());

        public Task<List<SimpleMediaCollection>> GetSimpleMediaCollections() =>
            _dbContext.SimpleMediaCollections.ToListAsync();

        public Task<List<MediaCollection>> GetAll() =>
            _dbContext.MediaCollections.ToListAsync();

        public Task<List<MediaCollectionSummary>> GetSummaries(string searchString) =>
            _dbContext.MediaCollectionSummaries.FromSqlRaw(
                @"SELECT mc.Id, mc.Name, Count(mismc.ItemsId) AS ItemCount, true AS IsSimple
                FROM MediaCollections mc
                    INNER JOIN SimpleMediaCollections smc ON smc.Id = mc.Id
                    LEFT OUTER JOIN MediaItemSimpleMediaCollection mismc ON mismc.SimpleMediaCollectionsId = mc.Id
                WHERE mc.Name LIKE {0}
                GROUP BY mc.Id, mc.Name
                UNION ALL
                SELECT mc.Id, mc.Name, Count(mi.Id) AS ItemCount, false AS IsSimple
                FROM MediaCollections mc
                    INNER JOIN TelevisionMediaCollections tmc ON tmc.Id = mc.Id
                    LEFT OUTER JOIN MediaItems mi ON (tmc.SeasonNumber IS NULL OR mi.Metadata_SeasonNumber = tmc.SeasonNumber)
                                                         AND mi.Metadata_Title = tmc.ShowTitle
                WHERE mc.Name LIKE {0}
                GROUP BY mc.Id, mc.Name",
                $"%{searchString}%").ToListAsync();

        public Task<Option<List<MediaItem>>> GetItems(int id) =>
            Get(id).MapT(
                collection => collection switch
                {
                    SimpleMediaCollection s => SimpleItems(s),
                    TelevisionMediaCollection t => TelevisionItems(t)
                }).Bind(x => x.Sequence());

        public Task<Option<List<MediaItem>>> GetSimpleMediaCollectionItems(int id) =>
            GetSimpleMediaCollection(id).MapT(SimpleItems).Bind(x => x.Sequence());

        public Task<Option<List<MediaItem>>> GetTelevisionMediaCollectionItems(int id) =>
            GetTelevisionMediaCollection(id).MapT(TelevisionItems).Bind(x => x.Sequence());

        public Task Update(SimpleMediaCollection collection)
        {
            _dbContext.SimpleMediaCollections.Update(collection);
            return _dbContext.SaveChangesAsync();
        }

        public async Task InsertOrIgnore(TelevisionMediaCollection collection)
        {
            if (!_dbContext.TelevisionMediaCollections.Any(
                existing => existing.ShowTitle == collection.ShowTitle &&
                            existing.SeasonNumber == collection.SeasonNumber))
            {
                await _dbContext.TelevisionMediaCollections.AddAsync(collection);
                await _dbContext.SaveChangesAsync();
            }
        }

        public Task<Unit> ReplaceItems(int collectionId, List<MediaItem> mediaItems) =>
            GetSimpleMediaCollection(collectionId).IfSomeAsync(
                async c =>
                {
                    await SimpleItems(c);

                    c.Items.Clear();
                    foreach (MediaItem mediaItem in mediaItems)
                    {
                        c.Items.Add(mediaItem);
                    }

                    _dbContext.SimpleMediaCollections.Update(c);
                    await _dbContext.SaveChangesAsync();
                });

        public async Task Delete(int mediaCollectionId)
        {
            MediaCollection mediaCollection = await _dbContext.MediaCollections.FindAsync(mediaCollectionId);
            _dbContext.MediaCollections.Remove(mediaCollection);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteEmptyTelevisionCollections()
        {
            List<int> ids = await _dbContext.GenericIntegerIds.FromSqlRaw(
                    @"SELECT mc.Id FROM MediaCollections mc
INNER JOIN TelevisionMediaCollections t on mc.Id = t.Id
WHERE NOT EXISTS
(SELECT 1 FROM MediaItems mi WHERE t.ShowTitle = mi.Metadata_Title AND (t.SeasonNumber IS NULL OR t.SeasonNumber = mi.Metadata_SeasonNumber))")
                .Map(i => i.Id)
                .ToListAsync();

            List<MediaCollection> toDelete =
                await _dbContext.MediaCollections.Where(mc => ids.Contains(mc.Id)).ToListAsync();
            _dbContext.MediaCollections.RemoveRange(toDelete);

            await _dbContext.SaveChangesAsync();
        }

        private async Task<List<MediaItem>> SimpleItems(SimpleMediaCollection collection)
        {
            await _dbContext.Entry(collection).Collection(c => c.Items).LoadAsync();
            return collection.Items.ToList();
        }

        private Task<List<MediaItem>> TelevisionItems(TelevisionMediaCollection collection) =>
            _dbContext.MediaItems
                .Filter(c => c.Metadata.Title == collection.ShowTitle)
                .Filter(c => collection.SeasonNumber == null || c.Metadata.SeasonNumber == collection.SeasonNumber)
                .ToListAsync();
    }
}
