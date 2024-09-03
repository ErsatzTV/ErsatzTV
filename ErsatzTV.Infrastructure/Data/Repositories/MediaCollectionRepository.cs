using System.Diagnostics.CodeAnalysis;
using Bugsnag;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Extensions;
using ErsatzTV.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class MediaCollectionRepository : IMediaCollectionRepository
{
    private readonly IClient _client;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchIndex _searchIndex;

    public MediaCollectionRepository(
        IClient client,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _client = client;
        _searchIndex = searchIndex;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(int playlistId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new Dictionary<PlaylistItem, List<MediaItem>>();

        Option<Playlist> maybePlaylist = await dbContext.Playlists
            .Include(p => p.Items)
            .SelectOneAsync(p => p.Id, p => p.Id == playlistId);

        foreach (PlaylistItem playlistItem in maybePlaylist.SelectMany(p => p.Items))
        {
            var mediaItems = new List<MediaItem>();

            switch (playlistItem.CollectionType)
            {
                case ProgramScheduleItemCollectionType.Collection:
                    foreach (int collectionId in Optional(playlistItem.CollectionId))
                    {
                        mediaItems.AddRange(await GetMovieItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetShowItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetSeasonItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetEpisodeItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetArtistItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetMusicVideoItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetOtherVideoItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetSongItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetImageItems(dbContext, collectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.TelevisionShow:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetShowItemsFromShowId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.TelevisionSeason:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetSeasonItemsFromSeasonId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Artist:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetArtistItemsFromArtistId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.MultiCollection:
                    foreach (int multiCollectionId in Optional(playlistItem.MultiCollectionId))
                    {
                        mediaItems.AddRange(await GetMultiCollectionItems(multiCollectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.SmartCollection:
                    foreach (int smartCollectionId in Optional(playlistItem.SmartCollectionId))
                    {
                        mediaItems.AddRange(await GetSmartCollectionItems(smartCollectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Movie:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetMovieItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Episode:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetEpisodeItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.MusicVideo:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetMusicVideoItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.OtherVideo:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetOtherVideoItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Song:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetSongItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Image:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetImageItems(dbContext, [mediaItemId]));
                    }

                    break;
            }

            result.Add(playlistItem, mediaItems);
        }

        return result;
    }

    public async Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(string groupName, string name)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Option<Playlist> maybePlaylist = await dbContext.Playlists
            .SelectOneAsync(p => p.Name, p => EF.Functions.Collate(p.Name, TvContext.CaseInsensitiveCollation) == name);

        foreach (Playlist playlist in maybePlaylist)
        {
            return await GetPlaylistItemMap(playlist.Id);
        }

        return [];
    }

    public async Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(Playlist playlist)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new Dictionary<PlaylistItem, List<MediaItem>>();

        foreach (PlaylistItem playlistItem in playlist.Items)
        {
            var mediaItems = new List<MediaItem>();

            switch (playlistItem.CollectionType)
            {
                case ProgramScheduleItemCollectionType.Collection:
                    foreach (int collectionId in Optional(playlistItem.CollectionId))
                    {
                        mediaItems.AddRange(await GetMovieItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetShowItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetSeasonItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetEpisodeItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetArtistItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetMusicVideoItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetOtherVideoItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetSongItems(dbContext, collectionId));
                        mediaItems.AddRange(await GetImageItems(dbContext, collectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.TelevisionShow:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetShowItemsFromShowId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.TelevisionSeason:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetSeasonItemsFromSeasonId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Artist:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetArtistItemsFromArtistId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.MultiCollection:
                    foreach (int multiCollectionId in Optional(playlistItem.MultiCollectionId))
                    {
                        mediaItems.AddRange(await GetMultiCollectionItems(multiCollectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.SmartCollection:
                    foreach (int smartCollectionId in Optional(playlistItem.SmartCollectionId))
                    {
                        mediaItems.AddRange(await GetSmartCollectionItems(smartCollectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Movie:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetMovieItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Episode:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetEpisodeItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.MusicVideo:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetMusicVideoItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.OtherVideo:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetOtherVideoItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Song:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetSongItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Image:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        mediaItems.AddRange(await GetImageItems(dbContext, [mediaItemId]));
                    }

                    break;
            }

            result.Add(playlistItem, mediaItems);
        }

        return result;
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

    public async Task<List<MediaItem>> GetItems(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new List<MediaItem>();

        result.AddRange(await GetMovieItems(dbContext, id));
        result.AddRange(await GetShowItems(dbContext, id));
        result.AddRange(await GetSeasonItems(dbContext, id));
        result.AddRange(await GetEpisodeItems(dbContext, id));
        result.AddRange(await GetArtistItems(dbContext, id));
        result.AddRange(await GetMusicVideoItems(dbContext, id));
        result.AddRange(await GetOtherVideoItems(dbContext, id));
        result.AddRange(await GetSongItems(dbContext, id));
        result.AddRange(await GetImageItems(dbContext, id));

        return result.Distinct().ToList();
    }

    public async Task<List<MediaItem>> GetCollectionItemsByName(string name)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Option<Collection> maybeCollection = await dbContext.Collections
            .SelectOneAsync(c => c.Name, c => EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation) == name);

        foreach (Collection collection in maybeCollection)
        {
            return await GetItems(collection.Id);
        }

        return [];
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
                result.AddRange(await GetImageItems(dbContext, collectionId));
            }

            foreach (int smartCollectionId in multiCollection.SmartCollections.Map(c => c.Id))
            {
                result.AddRange(await GetSmartCollectionItems(smartCollectionId));
            }
        }

        return result.DistinctBy(x => x.Id).ToList();
    }

    public async Task<List<MediaItem>> GetMultiCollectionItemsByName(string name)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Option<MultiCollection> maybeCollection = await dbContext.MultiCollections
            .SelectOneAsync(
                mc => mc.Name,
                mc => EF.Functions.Collate(mc.Name, TvContext.CaseInsensitiveCollation) == name);

        foreach (MultiCollection collection in maybeCollection)
        {
            return await GetMultiCollectionItems(collection.Id);
        }

        return [];
    }

    public async Task<List<MediaItem>> GetSmartCollectionItems(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Option<SmartCollection> maybeCollection = await dbContext.SmartCollections
            .SelectOneAsync(sc => sc.Id, sc => sc.Id == id);

        foreach (SmartCollection collection in maybeCollection)
        {
            return await GetSmartCollectionItems(collection.Query);
        }

        return [];
    }

    public async Task<List<MediaItem>> GetSmartCollectionItemsByName(string name)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Option<SmartCollection> maybeCollection = await dbContext.SmartCollections
            .SelectOneAsync(
                sc => sc.Name,
                sc => EF.Functions.Collate(sc.Name, TvContext.CaseInsensitiveCollation) == name);

        foreach (SmartCollection collection in maybeCollection)
        {
            return await GetSmartCollectionItems(collection.Query);
        }

        return [];
    }

    public async Task<List<MediaItem>> GetSmartCollectionItems(string query)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new List<MediaItem>();

        // elasticsearch doesn't like when we ask for a limit of zero, so use 10,000
        SearchResult searchResults = await _searchIndex.Search(_client, query, 0, 10_000);

        var movieIds = searchResults.Items
            .Filter(i => i.Type == LuceneSearchIndex.MovieType)
            .Map(i => i.Id)
            .ToList();
        result.AddRange(await GetMovieItems(dbContext, movieIds));

        foreach (int showId in searchResults.Items.Filter(i => i.Type == LuceneSearchIndex.ShowType).Map(i => i.Id))
        {
            result.AddRange(await GetShowItemsFromShowId(dbContext, showId));
        }

        foreach (int seasonId in searchResults.Items.Filter(i => i.Type == LuceneSearchIndex.SeasonType)
                     .Map(i => i.Id))
        {
            result.AddRange(await GetSeasonItemsFromSeasonId(dbContext, seasonId));
        }

        foreach (int artistId in searchResults.Items.Filter(i => i.Type == LuceneSearchIndex.ArtistType)
                     .Map(i => i.Id))
        {
            result.AddRange(await GetArtistItemsFromArtistId(dbContext, artistId));
        }

        var musicVideoIds = searchResults.Items
            .Filter(i => i.Type == LuceneSearchIndex.MusicVideoType)
            .Map(i => i.Id)
            .ToList();
        result.AddRange(await GetMusicVideoItems(dbContext, musicVideoIds));

        var episodeIds = searchResults.Items
            .Filter(i => i.Type == LuceneSearchIndex.EpisodeType)
            .Map(i => i.Id)
            .ToList();
        result.AddRange(await GetEpisodeItems(dbContext, episodeIds));

        var otherVideoIds = searchResults.Items
            .Filter(i => i.Type == LuceneSearchIndex.OtherVideoType)
            .Map(i => i.Id)
            .ToList();
        result.AddRange(await GetOtherVideoItems(dbContext, otherVideoIds));

        var songIds = searchResults.Items
            .Filter(i => i.Type == LuceneSearchIndex.SongType)
            .Map(i => i.Id)
            .ToList();
        result.AddRange(await GetSongItems(dbContext, songIds));

        var imageIds = searchResults.Items
            .Filter(i => i.Type == LuceneSearchIndex.ImageType)
            .Map(i => i.Id)
            .ToList();
        result.AddRange(await GetImageItems(dbContext, imageIds));

        return result.DistinctBy(x => x.Id).ToList();
    }

    public async Task<List<MediaItem>> GetShowItemsByShowGuids(List<string> guids)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new List<MediaItem>();

        System.Collections.Generic.HashSet<int> showIds = [];

        foreach (string guid in guids)
        {
            // don't search any more once we have a matching show
            if (showIds.Count > 0)
            {
                break;
            }

            List<int> nextIds = await dbContext.ShowMetadata
                .Filter(
                    sm => sm.Guids.Any(g => EF.Functions.Collate(g.Guid, TvContext.CaseInsensitiveCollation) == guid))
                .Map(sm => sm.ShowId)
                .ToListAsync();

            foreach (int showId in nextIds)
            {
                showIds.Add(showId);
            }
        }

        // multiple shows are not supported here, just use the first match
        foreach (int showId in showIds.HeadOrNone())
        {
            result.AddRange(await GetShowItemsFromShowId(dbContext, showId));
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
                                multiCollectionItem.CollectionId,
                                null,
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
                            multiCollectionItem.CollectionId,
                            null,
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
                        multiCollectionSmartItem.SmartCollectionId,
                        null,
                        items,
                        multiCollectionSmartItem.ScheduleAsGroup,
                        multiCollectionSmartItem.PlaybackOrder,
                        false));
            }
        }

        // remove duplicate items from ungrouped collections
        var toRemoveFrom = result.Filter(c => !c.ScheduleAsGroup).ToList();
        var scheduleAsGroupItemIds = result.Filter(c => c.ScheduleAsGroup)
            .SelectMany(c => c.MediaItems.Map(i => i.Id))
            .Distinct()
            .ToHashSet();

        foreach (CollectionWithItems collection in toRemoveFrom)
        {
            collection.MediaItems.RemoveAll(mi => scheduleAsGroupItemIds.Contains(mi.Id));
        }

        return result;
    }

    public async Task<List<MediaItem>> GetPlaylistItems(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new List<MediaItem>();

        Option<Playlist> maybePlaylist = await dbContext.Playlists
            .Include(p => p.Items)
            .SelectOneAsync(p => p.Id, p => p.Id == id);

        foreach (PlaylistItem playlistItem in maybePlaylist.SelectMany(p => p.Items))
        {
            switch (playlistItem.CollectionType)
            {
                case ProgramScheduleItemCollectionType.Collection:
                    foreach (int collectionId in Optional(playlistItem.CollectionId))
                    {
                        result.AddRange(await GetMovieItems(dbContext, collectionId));
                        result.AddRange(await GetShowItems(dbContext, collectionId));
                        result.AddRange(await GetSeasonItems(dbContext, collectionId));
                        result.AddRange(await GetEpisodeItems(dbContext, collectionId));
                        result.AddRange(await GetArtistItems(dbContext, collectionId));
                        result.AddRange(await GetMusicVideoItems(dbContext, collectionId));
                        result.AddRange(await GetOtherVideoItems(dbContext, collectionId));
                        result.AddRange(await GetSongItems(dbContext, collectionId));
                        result.AddRange(await GetImageItems(dbContext, collectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.TelevisionShow:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetShowItemsFromShowId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.TelevisionSeason:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetSeasonItemsFromSeasonId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Artist:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetArtistItemsFromArtistId(dbContext, mediaItemId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.MultiCollection:
                    foreach (int multiCollectionId in Optional(playlistItem.MultiCollectionId))
                    {
                        result.AddRange(await GetMultiCollectionItems(multiCollectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.SmartCollection:
                    foreach (int smartCollectionId in Optional(playlistItem.SmartCollectionId))
                    {
                        result.AddRange(await GetSmartCollectionItems(smartCollectionId));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Movie:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetMovieItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Episode:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetEpisodeItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.MusicVideo:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetMusicVideoItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.OtherVideo:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetOtherVideoItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Song:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetSongItems(dbContext, [mediaItemId]));
                    }

                    break;

                case ProgramScheduleItemCollectionType.Image:
                    foreach (int mediaItemId in Optional(playlistItem.MediaItemId))
                    {
                        result.AddRange(await GetImageItems(dbContext, [mediaItemId]));
                    }

                    break;
            }
        }

        return result.DistinctBy(x => x.Id).ToList();
    }

    public async Task<List<Movie>> GetMovie(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await GetMovieItems(dbContext, [id]);
    }

    public async Task<List<Episode>> GetEpisode(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await GetEpisodeItems(dbContext, [id]);
    }

    public async Task<List<MusicVideo>> GetMusicVideo(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await GetMusicVideoItems(dbContext, [id]);
    }

    public async Task<List<OtherVideo>> GetOtherVideo(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await GetOtherVideoItems(dbContext, [id]);
    }

    public async Task<List<Song>> GetSong(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await GetSongItems(dbContext, [id]);
    }

    public async Task<List<Image>> GetImage(int id)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await GetImageItems(dbContext, [id]);
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

    public async Task<List<int>> PlayoutIdsUsingCollection(int collectionId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<int>(
                @"SELECT DISTINCT p.PlayoutId
                    FROM PlayoutProgramScheduleAnchor p
                    WHERE p.CollectionId = @CollectionId",
                new { CollectionId = collectionId })
            .Map(result => result.ToList());
    }

    public async Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<int>(
                @"SELECT DISTINCT p.PlayoutId
                    FROM PlayoutProgramScheduleAnchor p
                    WHERE p.MultiCollectionId = @MultiCollectionId",
                new { MultiCollectionId = multiCollectionId })
            .Map(result => result.ToList());
    }

    public async Task<List<int>> PlayoutIdsUsingSmartCollection(int smartCollectionId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<int>(
                @"SELECT DISTINCT p.PlayoutId
                    FROM PlayoutProgramScheduleAnchor p
                    WHERE p.SmartCollectionId = @SmartCollectionId",
                new { SmartCollectionId = smartCollectionId })
            .Map(result => result.ToList());
    }

    public async Task<bool> IsCustomPlaybackOrder(int collectionId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QuerySingleAsync<bool>(
            @"SELECT IFNULL(MIN(UseCustomPlaybackOrder), 0) FROM Collection WHERE Id = @CollectionId",
            new { CollectionId = collectionId });
    }

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
            // TODO: get playlist name
            _ => None
        };
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public List<CollectionWithItems> GroupIntoFakeCollections(List<MediaItem> items, string fakeKey = null)
    {
        int id = -1;
        var result = new List<CollectionWithItems>();

        var showCollections = new Dictionary<int, List<MediaItem>>();
        foreach (Episode episode in items.OfType<Episode>())
        {
            List<MediaItem> list = showCollections.TryGetValue(episode.Season.ShowId, out List<MediaItem> collection)
                ? collection
                : new List<MediaItem>();

            if (list.All(i => i.Id != episode.Id))
            {
                list.Add(episode);
            }

            showCollections[episode.Season.ShowId] = list;
        }

        foreach ((int showId, List<MediaItem> list) in showCollections)
        {
            result.Add(
                new CollectionWithItems(
                    showId,
                    0,
                    fakeKey,
                    list,
                    true,
                    PlaybackOrder.Chronological,
                    false));
        }

        var artistCollections = new Dictionary<int, List<MediaItem>>();
        foreach (MusicVideo musicVideo in items.OfType<MusicVideo>())
        {
            List<MediaItem> list = artistCollections.TryGetValue(musicVideo.ArtistId, out List<MediaItem> collection)
                ? collection
                : new List<MediaItem>();

            if (list.All(i => i.Id != musicVideo.Id))
            {
                list.Add(musicVideo);
            }

            artistCollections[musicVideo.ArtistId] = list;
        }

        foreach ((int artistId, List<MediaItem> list) in artistCollections)
        {
            result.Add(
                new CollectionWithItems(
                    0,
                    artistId,
                    fakeKey,
                    list,
                    true,
                    PlaybackOrder.Chronological,
                    false));
        }

        var allArtists = items.OfType<Song>()
            .SelectMany(s => s.SongMetadata)
            .Map(sm => sm.AlbumArtists.HeadOrNone().Match(aa => aa, string.Empty))
            .Distinct()
            .ToList();

        if (!allArtists.Contains(string.Empty))
        {
            allArtists.Add(string.Empty);
        }

        var songArtistCollections = new Dictionary<int, List<MediaItem>>();
        foreach (Song song in items.OfType<Song>())
        {
            string firstArtist = song.SongMetadata
                .SelectMany(sm => sm.AlbumArtists)
                .HeadOrNone()
                .Match(aa => aa, string.Empty);

            int key = allArtists.IndexOf(firstArtist);

            List<MediaItem> list = songArtistCollections.TryGetValue(key, out List<MediaItem> collection)
                ? collection
                : [];

            if (list.All(i => i.Id != song.Id))
            {
                list.Add(song);
            }

            songArtistCollections[key] = list;
        }

        foreach ((int index, List<MediaItem> list) in songArtistCollections)
        {
            result.Add(
                new CollectionWithItems(
                    id,
                    id,
                    $"{fakeKey}:artist:{allArtists[index]}",
                    list,
                    true,
                    PlaybackOrder.Chronological,
                    false));

            id--;
        }

        result.Add(
            new CollectionWithItems(
                id,
                id,
                fakeKey,
                items.OfType<Movie>().Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.Chronological,
                false));
        id--;

        result.Add(
            new CollectionWithItems(
                id,
                id,
                fakeKey,
                items.OfType<OtherVideo>().Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.Chronological,
                false));

        return result.Filter(c => c.MediaItems.Count != 0).ToList();
    }

    private static async Task<List<Movie>> GetMovieItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM CollectionItem ci
            INNER JOIN Movie m ON m.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
            new { CollectionId = collectionId });

        return await GetMovieItems(dbContext, ids);
    }

    private static Task<List<Movie>> GetMovieItems(TvContext dbContext, IEnumerable<int> movieIds) =>
        dbContext.Movies
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Filter(m => movieIds.Contains(m.Id))
            .ToListAsync();

    private static async Task<List<MusicVideo>> GetArtistItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
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
            .ThenInclude(mvm => mvm.Subtitles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Filter(m => musicVideoIds.Contains(m.Id))
            .ToListAsync();

    private static async Task<List<MusicVideo>> GetArtistItemsFromArtistId(TvContext dbContext, int artistId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT MusicVideo.Id FROM Artist
            INNER JOIN MusicVideo on Artist.Id = MusicVideo.ArtistId
            WHERE Artist.Id = @ArtistId",
            new { ArtistId = artistId });

        return await GetArtistItemsFromMusicVideoIds(dbContext, ids);
    }

    private static async Task<List<MusicVideo>> GetMusicVideoItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
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
            .ThenInclude(mvm => mvm.Subtitles)
            .Include(m => m.MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Artists)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Filter(m => musicVideoIds.Contains(m.Id))
            .ToListAsync();

    private static async Task<List<OtherVideo>> GetOtherVideoItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT o.Id FROM CollectionItem ci
            INNER JOIN OtherVideo o ON o.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
            new { CollectionId = collectionId });

        return await GetOtherVideoItems(dbContext, ids);
    }

    private static Task<List<OtherVideo>> GetOtherVideoItems(TvContext dbContext, IEnumerable<int> otherVideoIds) =>
        dbContext.OtherVideos
            .Include(m => m.OtherVideoMetadata)
            .ThenInclude(ovm => ovm.Subtitles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Filter(m => otherVideoIds.Contains(m.Id))
            .ToListAsync();

    private static async Task<List<Song>> GetSongItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT s.Id FROM CollectionItem ci
            INNER JOIN Song s ON s.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
            new { CollectionId = collectionId });

        return await GetSongItems(dbContext, ids);
    }

    private static Task<List<Song>> GetSongItems(TvContext dbContext, IEnumerable<int> songIds) =>
        dbContext.Songs
            .Include(m => m.SongMetadata)
            .ThenInclude(s => s.Subtitles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Filter(m => songIds.Contains(m.Id))
            .ToListAsync();

    private static async Task<List<Image>> GetImageItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT i.Id FROM CollectionItem ci
            INNER JOIN Image i ON i.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
            new { CollectionId = collectionId });

        return await GetImageItems(dbContext, ids);
    }

    private static Task<List<Image>> GetImageItems(TvContext dbContext, IEnumerable<int> songIds) =>
        dbContext.Images
            .Include(m => m.ImageMetadata)
            .ThenInclude(im => im.Subtitles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Filter(m => songIds.Contains(m.Id))
            .ToListAsync();

    private static async Task<List<Episode>> GetShowItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            """
            SELECT Episode.Id FROM CollectionItem ci
                INNER JOIN `Show` ON `Show`.Id = ci.MediaItemId
                INNER JOIN Season ON Season.ShowId = `Show`.Id
                INNER JOIN Episode ON Episode.SeasonId = Season.Id
                WHERE ci.CollectionId = @CollectionId
            """,
            new { CollectionId = collectionId });

        return await GetShowItemsFromEpisodeIds(dbContext, ids);
    }

    private static Task<List<Episode>> GetShowItemsFromEpisodeIds(TvContext dbContext, IEnumerable<int> episodeIds) =>
        dbContext.Episodes
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Filter(e => episodeIds.Contains(e.Id))
            .ToListAsync();

    private static async Task<List<Episode>> GetShowItemsFromShowId(TvContext dbContext, int showId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT Episode.Id FROM `Show`
            INNER JOIN Season ON Season.ShowId = `Show`.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE `Show`.Id = @ShowId",
            new { ShowId = showId });

        return await GetShowItemsFromEpisodeIds(dbContext, ids);
    }

    private static async Task<List<Episode>> GetSeasonItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
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
            .ThenInclude(em => em.Subtitles)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Filter(e => episodeIds.Contains(e.Id))
            .ToListAsync();

    private static async Task<List<Episode>> GetSeasonItemsFromSeasonId(TvContext dbContext, int seasonId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT Episode.Id FROM Season
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE Season.Id = @SeasonId",
            new { SeasonId = seasonId });

        return await GetSeasonItemsFromEpisodeIds(dbContext, ids);
    }

    private static async Task<List<Episode>> GetEpisodeItems(TvContext dbContext, int collectionId)
    {
        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT Episode.Id FROM CollectionItem ci
            INNER JOIN Episode ON Episode.Id = ci.MediaItemId
            WHERE ci.CollectionId = @CollectionId",
            new { CollectionId = collectionId });

        return await GetEpisodeItems(dbContext, ids);
    }

    private static Task<List<Episode>> GetEpisodeItems(TvContext dbContext, IEnumerable<int> episodeIds) =>
        dbContext.Episodes
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
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
