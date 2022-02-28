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
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.Collections
                .Include(c => c.CollectionItems)
                .OrderBy(c => c.Id)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);
        }

        public async Task<List<MediaItem>> GetItems(int collectionId)
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            var result = new List<MediaItem>();

            result.AddRange(await GetMovieItems(dbContext, collectionId));
            result.AddRange(await GetShowItems(dbContext, collectionId));
            result.AddRange(await GetSeasonItems(dbContext, collectionId));
            result.AddRange(await GetEpisodeItems(dbContext, collectionId));
            result.AddRange(await GetArtistItems(dbContext, collectionId));
            result.AddRange(await GetMusicVideoItems(dbContext, collectionId));
            result.AddRange(await GetOtherVideoItems(dbContext, collectionId));
            result.AddRange(await GetSongItems(dbContext, collectionId));

            return result.Distinct().ToList();
        }

        public async Task<List<MediaItem>> GetMultiCollectionItems(int id)
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            var result = new List<MediaItem>();

            Option<MultiCollection> maybeMultiCollection = await dbContext.MultiCollections
                .Include(mc => mc.Collections)
                .Include(mc => mc.SmartCollections)
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
                    result.AddRange(await GetOtherVideoItems(dbContext, collectionId));
                    result.AddRange(await GetSongItems(dbContext, collectionId));
                }

                foreach (int smartCollectionId in multiCollection.SmartCollections.Map(c => c.Id))
                {
                    result.AddRange(await GetSmartCollectionItems(smartCollectionId));
                }
            }
            
            return result.Distinct().ToList();
        }

        public async Task<List<MediaItem>> GetSmartCollectionItems(int id)
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

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

                foreach (int showId in searchResults.Items.Filter(i => i.Type == SearchIndex.ShowType).Map(i => i.Id))
                {
                    result.AddRange(await GetShowItemsFromShowId(dbContext, showId));
                }

                foreach (int seasonId in searchResults.Items.Filter(i => i.Type == SearchIndex.SeasonType)
                    .Map(i => i.Id))
                {
                    result.AddRange(await GetSeasonItemsFromSeasonId(dbContext, seasonId));
                }

                foreach (int artistId in searchResults.Items.Filter(i => i.Type == SearchIndex.ArtistType)
                    .Map(i => i.Id))
                {
                    result.AddRange(await GetArtistItemsFromArtistId(dbContext, artistId));
                }

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

                var otherVideoIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.OtherVideoType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetOtherVideoItems(dbContext, otherVideoIds));

                var songIds = searchResults.Items
                    .Filter(i => i.Type == SearchIndex.SongType)
                    .Map(i => i.Id)
                    .ToList();
                result.AddRange(await GetSongItems(dbContext, songIds));
            }

            return result;
        }

        public async Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id)
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            var result = new List<CollectionWithItems>();

            Option<MultiCollection> maybeMultiCollection = await dbContext.MultiCollections
                .Include(mc => mc.Collections)
                .Include(mc => mc.SmartCollections)
                .Include(mc => mc.MultiCollectionItems)
                .ThenInclude(mci => mci.Collection)
                .Include(mc => mc.MultiCollectionSmartItems)
                .ThenInclude(mci => mci.SmartCollection)
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

                foreach (MultiCollectionSmartItem multiCollectionSmartItem in multiCollection.MultiCollectionSmartItems)
                {
                    List<MediaItem> items = await GetSmartCollectionItems(multiCollectionSmartItem.SmartCollectionId);

                    result.Add(
                        new CollectionWithItems(
                            multiCollectionSmartItem.SmartCollectionId,
                            items,
                            multiCollectionSmartItem.ScheduleAsGroup,
                            multiCollectionSmartItem.PlaybackOrder,
                            false));
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

        public async Task<List<CollectionWithItems>> GetFakeMultiCollectionCollections(
            int? collectionId,
            int? smartCollectionId)
        {
            var items = new List<MediaItem>();

            if (collectionId.HasValue)
            {
                items = await GetItems(collectionId.Value);
            }

            if (smartCollectionId.HasValue)
            {
                items = await GetSmartCollectionItems(smartCollectionId.Value);
            }

            return GroupIntoFakeCollections(items);
        }

        private static List<CollectionWithItems> GroupIntoFakeCollections(List<MediaItem> items)
        {
            int id = -1;
            var result = new List<CollectionWithItems>();

            var showCollections = new Dictionary<int, List<MediaItem>>();
            foreach (Episode episode in items.OfType<Episode>())
            {
                List<MediaItem> list = showCollections.ContainsKey(episode.Season.ShowId)
                    ? showCollections[episode.Season.ShowId]
                    : new List<MediaItem>();

                if (list.All(i => i.Id != episode.Id))
                {
                    list.Add(episode);
                }

                showCollections[episode.Season.ShowId] = list;
            }

            foreach ((int _, List<MediaItem> list) in showCollections)
            {
                result.Add(
                    new CollectionWithItems(
                        id--,
                        list,
                        true,
                        PlaybackOrder.Chronological,
                        false));
            }
            
            var artistCollections = new Dictionary<int, List<MediaItem>>();
            foreach (MusicVideo musicVideo in items.OfType<MusicVideo>())
            {
                List<MediaItem> list = artistCollections.ContainsKey(musicVideo.ArtistId)
                    ? artistCollections[musicVideo.ArtistId]
                    : new List<MediaItem>();

                if (list.All(i => i.Id != musicVideo.Id))
                {
                    list.Add(musicVideo);
                }

                artistCollections[musicVideo.ArtistId] = list;
            }

            foreach ((int _, List<MediaItem> list) in artistCollections)
            {
                result.Add(
                    new CollectionWithItems(
                        id--,
                        list,
                        true,
                        PlaybackOrder.Chronological,
                        false));
            }

            var allArtists = items.OfType<Song>()
                .SelectMany(s => s.SongMetadata)
                .Map(sm => sm.Artist)
                .Distinct()
                .ToList();

            if (!allArtists.Contains(string.Empty))
            {
                allArtists.Add(string.Empty);
            }

            var songArtistCollections = new Dictionary<int, List<MediaItem>>();
            foreach (Song song in items.OfType<Song>())
            {
                int key = allArtists.IndexOf(song.SongMetadata.HeadOrNone().Match(sm => sm.Artist, string.Empty));
                
                List<MediaItem> list = songArtistCollections.ContainsKey(key)
                        ? songArtistCollections[key]
                        : new List<MediaItem>();

                if (list.All(i => i.Id != song.Id))
                {
                    list.Add(song);
                }

                songArtistCollections[key] = list;
            }

            foreach ((int _, List<MediaItem> list) in songArtistCollections)
            {
                result.Add(
                    new CollectionWithItems(
                        id--,
                        list,
                        true,
                        PlaybackOrder.Chronological,
                        false));
            }

            result.Add(
                new CollectionWithItems(
                    id,
                    items.OfType<Movie>().Cast<MediaItem>().ToList(),
                    true,
                    PlaybackOrder.Chronological,
                    false));

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

        public async Task<Option<string>> GetNameFromKey(CollectionKey emptyCollection)
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return emptyCollection.CollectionType switch
            {
                ProgramScheduleItemCollectionType.Artist => await dbContext.Artists.Include(a => a.ArtistMetadata)
                    .SelectOneAsync(a => a.Id, a => a.Id == emptyCollection.MediaItemId.Value)
                    .MapT(a => a.ArtistMetadata.Head().Title),
                ProgramScheduleItemCollectionType.Collection => await dbContext.Collections
                    .SelectOneAsync(c => c.Id, c => c.Id == emptyCollection.CollectionId.Value)
                    .MapT(c => c.Name),
                ProgramScheduleItemCollectionType.MultiCollection => await dbContext.MultiCollections
                    .SelectOneAsync(c => c.Id, c => c.Id == emptyCollection.MultiCollectionId.Value)
                    .MapT(c => c.Name),
                ProgramScheduleItemCollectionType.SmartCollection => await dbContext.SmartCollections
                    .SelectOneAsync(c => c.Id, c => c.Id == emptyCollection.SmartCollectionId.Value)
                    .MapT(c => c.Name),
                ProgramScheduleItemCollectionType.TelevisionSeason => await dbContext.Seasons
                    .Include(s => s.SeasonMetadata)
                    .Include(s => s.Show)
                    .ThenInclude(s => s.ShowMetadata)
                    .SelectOneAsync(a => a.Id, a => a.Id == emptyCollection.MediaItemId.Value)
                    .MapT(s => $"{s.Show.ShowMetadata.Head().Title} Season {s.SeasonNumber}"),
                ProgramScheduleItemCollectionType.TelevisionShow => await dbContext.Shows.Include(s => s.ShowMetadata)
                    .SelectOneAsync(a => a.Id, a => a.Id == emptyCollection.MediaItemId.Value)
                    .MapT(s => s.ShowMetadata.Head().Title),
                _ => None
            };
        }

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
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
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

            return await GetArtistItemsFromMusicVideoIds(dbContext, ids);
        }

        private static Task<List<MusicVideo>> GetArtistItemsFromMusicVideoIds(
            TvContext dbContext,
            IEnumerable<int> musicVideoIds) =>
            dbContext.MusicVideos
                .Include(m => m.Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .Include(m => m.MusicVideoMetadata)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Filter(m => musicVideoIds.Contains(m.Id))
                .ToListAsync();
        
        private async Task<List<MusicVideo>> GetArtistItemsFromArtistId(TvContext dbContext, int artistId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT MusicVideo.Id FROM Artist
            INNER JOIN MusicVideo on Artist.Id = MusicVideo.ArtistId
            WHERE Artist.Id = @ArtistId",
                new { ArtistId = artistId });

            return await GetArtistItemsFromMusicVideoIds(dbContext, ids);
        }

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
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Filter(m => musicVideoIds.Contains(m.Id))
                .ToListAsync();
        
        private async Task<List<OtherVideo>> GetOtherVideoItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT o.Id FROM CollectionItem ci
            INNER JOIN OtherVideo o ON o.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await GetOtherVideoItems(dbContext, ids);
        }

        private static Task<List<OtherVideo>> GetOtherVideoItems(TvContext dbContext, IEnumerable<int> otherVideoIds) =>
            dbContext.OtherVideos
                .Include(m => m.OtherVideoMetadata)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Filter(m => otherVideoIds.Contains(m.Id))
                .ToListAsync();

        private async Task<List<Song>> GetSongItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT s.Id FROM CollectionItem ci
            INNER JOIN Song s ON s.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await GetSongItems(dbContext, ids);
        }

        private static Task<List<Song>> GetSongItems(TvContext dbContext, IEnumerable<int> songIds) =>
            dbContext.Songs
                .Include(m => m.SongMetadata)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Filter(m => songIds.Contains(m.Id))
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

            return await GetShowItemsFromEpisodeIds(dbContext, ids);
        }
        
        private static Task<List<Episode>> GetShowItemsFromEpisodeIds(TvContext dbContext, IEnumerable<int> episodeIds) =>
            dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => episodeIds.Contains(e.Id))
                .ToListAsync();

        private async Task<List<Episode>> GetShowItemsFromShowId(TvContext dbContext, int showId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM Show
            INNER JOIN Season ON Season.ShowId = Show.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE Show.Id = @ShowId",
                new { ShowId = showId });

            return await GetShowItemsFromEpisodeIds(dbContext, ids);
        }

        private async Task<List<Episode>> GetSeasonItems(TvContext dbContext, int collectionId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM CollectionItem ci
            INNER JOIN Season ON Season.Id = ci.MediaItemId
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE ci.CollectionId = @CollectionId",
                new { CollectionId = collectionId });

            return await GetSeasonItemsFromEpisodeIds(dbContext, ids);
        }
        
        private static Task<List<Episode>> GetSeasonItemsFromEpisodeIds(TvContext dbContext, IEnumerable<int> episodeIds) =>
            dbContext.Episodes
                .Include(e => e.EpisodeMetadata)
                .Include(e => e.MediaVersions)
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => episodeIds.Contains(e.Id))
                .ToListAsync();
        
        private async Task<List<Episode>> GetSeasonItemsFromSeasonId(TvContext dbContext, int seasonId)
        {
            IEnumerable<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT Episode.Id FROM Season
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE Season.Id = @SeasonId",
                new { SeasonId = seasonId });

            return await GetSeasonItemsFromEpisodeIds(dbContext, ids);
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
                .ThenInclude(mv => mv.Chapters)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(e => episodeIds.Contains(e.Id))
                .ToListAsync();
    }
}
