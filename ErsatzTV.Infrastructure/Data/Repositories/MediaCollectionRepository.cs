using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Extensions;
using ErsatzTV.Infrastructure.Search;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaCollectionRepository : IMediaCollectionRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ISearchIndex _searchIndex;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MediaCollectionRepository(
            ISearchIndex searchIndex,
            IDbContextFactory<TvContext> dbContextFactory,
            IDbConnection dbConnection)
        {
            _searchIndex = searchIndex;
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

        public async Task<List<MediaItem>> GetSmartCollectionItems(int id)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            var result = new List<MediaItem>();

            Option<SmartCollection> maybeCollection = await dbContext.SmartCollections
                .SelectOneAsync(sc => sc.Id, sc => sc.Id == id);

            foreach (SmartCollection collection in maybeCollection)
            {
                SearchResult searchResults = await _searchIndex.Search(collection.Query, 0, 0);
                
                var movieIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.MovieType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetMovieItems(dbContext, movieIds));

                var showIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.ShowType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetShowItems(dbContext, showIds));

                var artistIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.ArtistType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetArtistItems(dbContext, artistIds));
                
                var musicVideoIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.MusicVideoType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetMusicVideoItems(dbContext, musicVideoIds));
                
                var episodeIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.EpisodeType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetEpisodeItems(dbContext, episodeIds));
            }

            return result;
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

        public Task<List<int>> PlayoutIdsUsingSmartCollection(int smartCollectionId) =>
            _dbConnection.QueryAsync<int>(
                    @"SELECT DISTINCT p.PlayoutId
                    FROM PlayoutProgramScheduleAnchor p
                    WHERE p.SmartCollectionId = @SmartCollectionId",
                    new { SmartCollectionId = smartCollectionId })
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

            return await GetMovieItems(dbContext, ids);
        }
        
        private static Task<List<Movie>> GetMovieItems(TvContext dbContext, IEnumerable<int> movieIds) =>
            dbContext.Movies
                .Include(m => m.MovieMetadata)
                .Include(m => m.MediaVersions)
                .Filter(m => movieIds.Contains(m.Id))
                .ToListAsync();

        private async Task<List<MusicVideo>> GetArtistItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT MusicVideo.Id FROM CollectionItem ci
            INNER JOIN Artist on Artist.Id = ci.MediaItemId
            INNER JOIN MusicVideo on Artist.Id = MusicVideo.ArtistId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await GetArtistItems(dbContext, ids);
        }

        private static Task<List<MusicVideo>> GetArtistItems(TvContext dbContext, IEnumerable<int> artistIds) =>
            dbContext.MusicVideos
                .Include(m => m.Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .Include(m => m.MusicVideoMetadata)
                .Include(m => m.MediaVersions)
                .Filter(m => artistIds.Contains(m.Id))
                .ToListAsync();

        private async Task<List<MusicVideo>> GetMusicVideoItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM CollectionItem ci
            INNER JOIN MusicVideo m ON m.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await GetMusicVideoItems(dbContext, ids);
        }
        
        private static Task<List<MusicVideo>> GetMusicVideoItems(TvContext dbContext, IEnumerable<int> musicVideoIds) =>
            dbContext.MusicVideos
                .Include(m => m.Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .Include(m => m.MusicVideoMetadata)
                .Include(m => m.MediaVersions)
                .Filter(m => musicVideoIds.Contains(m.Id))
                .ToListAsync();

        private async Task<List<Episode>> GetShowItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM CollectionItem ci
            INNER JOIN Show ON Show.Id = ci.MediaItemId
            INNER JOIN Season ON Season.ShowId = Show.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await GetShowItems(dbContext, ids);
        }
        
        private static Task<List<Episode>> GetShowItems(TvContext dbContext, IEnumerable<int> showIds) =>
            dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => showIds.Contains(e.Id))
                .ToListAsync();

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

            return await GetEpisodeItems(dbContext, ids);
        }
        
        private static Task<List<Episode>> GetEpisodeItems(TvContext dbContext, IEnumerable<int> episodeIds) =>
            dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => episodeIds.Contains(e.Id))
                .ToListAsync();
    }
}
