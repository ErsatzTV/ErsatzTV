using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaCollectionRepository : IMediaCollectionRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MediaCollectionRepository(
            IDbContextFactory<TvContext> dbContextFactory,
            IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Collections
                .Include(c => c.CollectionItems)
                .OrderBy(c => c.Id)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);
        }

        public async Task<List<MediaItem>> GetItems(int collectionId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            var result = new List<MediaItem>();

            result.AddRange(await GetMovieItems(dbContext, collectionId));
            result.AddRange(await GetShowItems(dbContext, collectionId));
            result.AddRange(await GetSeasonItems(dbContext, collectionId));
            result.AddRange(await GetEpisodeItems(dbContext, collectionId));
            result.AddRange(await GetArtistItems(dbContext, collectionId));
            result.AddRange(await GetMusicVideoItems(dbContext, collectionId));

            return result.Distinct().ToList();
        }

        public async Task<List<MediaItem>> GetMultiCollectionItems(int id)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            var result = new List<MediaItem>();

            Option<MultiCollection> maybeMultiCollection = await dbContext.MultiCollections
                .Include(mc => mc.Collections)
                .SelectOneAsync(mc => mc.Id, mc => mc.Id == id);

            foreach (MultiCollection multiCollection in maybeMultiCollection)
            {
                foreach (int collectionId in multiCollection.Collections.Map(c => c.Id))
                {
                    result.AddRange(await GetMovieItems(dbContext, collectionId));
                    result.AddRange(await GetShowItems(dbContext, collectionId));
                    result.AddRange(await GetSeasonItems(dbContext, collectionId));
                    result.AddRange(await GetEpisodeItems(dbContext, collectionId));
                    result.AddRange(await GetArtistItems(dbContext, collectionId));
                    result.AddRange(await GetMusicVideoItems(dbContext, collectionId));
                }
            }
            
            return result.Distinct().ToList();
        }

        public async Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            var result = new List<CollectionWithItems>();

            Option<MultiCollection> maybeMultiCollection = await dbContext.MultiCollections
                .Include(mc => mc.Collections)
                .Include(mc => mc.MultiCollectionItems)
                .ThenInclude(mci => mci.Collection)
                .SelectOneAsync(mc => mc.Id, mc => mc.Id == id);

            foreach (MultiCollection multiCollection in maybeMultiCollection)
            {
                foreach (MultiCollectionItem multiCollectionItem in multiCollection.MultiCollectionItems)
                {
                    List<MediaItem> items = await GetItems(multiCollectionItem.CollectionId);

                    if (multiCollectionItem.Collection.UseCustomPlaybackOrder)
                    {
                        foreach (Collection collection in await GetCollectionWithCollectionItemsUntracked(
                            multiCollectionItem.CollectionId))
                        {
                            var sortedItems = collection.CollectionItems
                                .OrderBy(ci => ci.CustomIndex)
                                .Map(ci => items.First(i => i.Id == ci.MediaItemId))
                                .ToList();

                            result.Add(
                                new CollectionWithItems(
                                    multiCollectionItem.CollectionId,
                                    sortedItems,
                                    multiCollectionItem.ScheduleAsGroup,
                                    multiCollectionItem.PlaybackOrder,
                                    multiCollectionItem.Collection.UseCustomPlaybackOrder));
                        }
                    }
                    else
                    {
                        result.Add(
                            new CollectionWithItems(
                                multiCollectionItem.CollectionId,
                                items,
                                multiCollectionItem.ScheduleAsGroup,
                                multiCollectionItem.PlaybackOrder,
                                multiCollectionItem.Collection.UseCustomPlaybackOrder));
                    }
                }
            }

            // remove duplicate items from ungrouped collections
            var toRemoveFrom = result.Filter(c => !c.ScheduleAsGroup).ToList();
            var toRemove = result.Filter(c => c.ScheduleAsGroup)
                .SelectMany(c => c.MediaItems.Map(i => i.Id))
                .Distinct()
                .ToList();

            foreach (CollectionWithItems collection in toRemoveFrom)
            {
                collection.MediaItems.RemoveAll(mi => toRemove.Contains(mi.Id));
            }

            return result;
        }

        public Task<List<int>> PlayoutIdsUsingCollection(int collectionId) =>
            _dbConnection.QueryAsync<int>(
                    @"SELECT DISTINCT p.PlayoutId
                    FROM PlayoutProgramScheduleAnchor p
                    WHERE p.CollectionId = @CollectionId",
                    new { CollectionId = collectionId })
                .Map(result => result.ToList());

        public Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId) =>
            _dbConnection.QueryAsync<int>(
                    @"SELECT DISTINCT p.PlayoutId
                    FROM PlayoutProgramScheduleAnchor p
                    WHERE p.MultiCollectionId = @MultiCollectionId",
                    new { MultiCollectionId = multiCollectionId })
                .Map(result => result.ToList());

        public Task<bool> IsCustomPlaybackOrder(int collectionId) =>
            _dbConnection.QuerySingleAsync<bool>(
                @"SELECT IFNULL(MIN(UseCustomPlaybackOrder), 0) FROM Collection WHERE Id = @CollectionId",
                new { CollectionId = collectionId });

        private async Task<List<Movie>> GetMovieItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM CollectionItem ci
            INNER JOIN Movie m ON m.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await dbContext.Movies
                .Include(m => m.MovieMetadata)
                .Include(m => m.MediaVersions)
                .Filter(m => ids.Contains(m.Id))
                .ToListAsync();
        }

        private async Task<List<MusicVideo>> GetArtistItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT MusicVideo.Id FROM CollectionItem ci
            INNER JOIN Artist on Artist.Id = ci.MediaItemId
            INNER JOIN MusicVideo on Artist.Id = MusicVideo.ArtistId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await dbContext.MusicVideos
                .Include(m => m.Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .Include(m => m.MusicVideoMetadata)
                .Include(m => m.MediaVersions)
                .Filter(m => ids.Contains(m.Id))
                .ToListAsync();
        }


        private async Task<List<MusicVideo>> GetMusicVideoItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM CollectionItem ci
            INNER JOIN MusicVideo m ON m.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await dbContext.MusicVideos
                .Include(m => m.Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .Include(m => m.MusicVideoMetadata)
                .Include(m => m.MediaVersions)
                .Filter(m => ids.Contains(m.Id))
                .ToListAsync();
        }

        private async Task<List<Episode>> GetShowItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM CollectionItem ci
            INNER JOIN Show ON Show.Id = ci.MediaItemId
            INNER JOIN Season ON Season.ShowId = Show.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        private async Task<List<Episode>> GetSeasonItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM CollectionItem ci
            INNER JOIN Season ON Season.Id = ci.MediaItemId
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        private async Task<List<Episode>> GetEpisodeItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM CollectionItem ci
            INNER JOIN Episode ON Episode.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => ids.Contains(e.Id))
                .ToListAsync();
        }
    }
}
