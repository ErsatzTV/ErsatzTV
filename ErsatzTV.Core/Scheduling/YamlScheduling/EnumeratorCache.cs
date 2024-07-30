using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class EnumeratorCache(IMediaCollectionRepository mediaCollectionRepository, ILogger logger)
{
    private readonly Dictionary<string, List<MediaItem>> _mediaItems = new();
    private readonly Dictionary<string, IMediaCollectionEnumerator> _enumerators = new();

    public System.Collections.Generic.HashSet<string> MissingContentKeys { get; } = [];

    public List<MediaItem> MediaItemsForContent(string contentKey) =>
        _mediaItems.TryGetValue(contentKey, out List<MediaItem> items) ? items : [];

    public async Task<Option<IMediaCollectionEnumerator>> GetCachedEnumeratorForContent(
        YamlPlayoutContext context,
        string contentKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentKey))
        {
            return Option<IMediaCollectionEnumerator>.None;
        }

        if (!_enumerators.TryGetValue(contentKey, out IMediaCollectionEnumerator enumerator))
        {
            Option<IMediaCollectionEnumerator> maybeEnumerator =
                await GetEnumeratorForContent(context, contentKey, cancellationToken);

            if (maybeEnumerator.IsNone)
            {
                return Option<IMediaCollectionEnumerator>.None;
            }

            foreach (IMediaCollectionEnumerator e in maybeEnumerator)
            {
                enumerator = e;
                _enumerators.Add(contentKey, enumerator);
            }
        }

        return Some(enumerator);
    }

    private async Task<Option<IMediaCollectionEnumerator>> GetEnumeratorForContent(
        YamlPlayoutContext context,
        string contentKey,
        CancellationToken cancellationToken)
    {
        int index = context.Definition.Content.FindIndex(c => c.Key == contentKey);
        if (index < 0)
        {
            return Option<IMediaCollectionEnumerator>.None;
        }

        List<MediaItem> items = [];

        YamlPlayoutContentItem content = context.Definition.Content[index];
        switch (content)
        {
            case YamlPlayoutContentSearchItem search:
                items = await mediaCollectionRepository.GetSmartCollectionItems(search.Query);
                break;
            case YamlPlayoutContentShowItem show:
                items = await mediaCollectionRepository.GetShowItemsByShowGuids(
                    show.Guids.Map(g => $"{g.Source}://{g.Value}").ToList());
                break;
            case YamlPlayoutContentCollectionItem collection:
                items = await mediaCollectionRepository.GetCollectionItemsByName(collection.Collection);
                break;
            case YamlPlayoutContentSmartCollectionItem smartCollection:
                items = await mediaCollectionRepository.GetSmartCollectionItemsByName(smartCollection.SmartCollection);
                break;
            case YamlPlayoutContentMultiCollectionItem multiCollection:
                items = await mediaCollectionRepository.GetMultiCollectionItemsByName(multiCollection.MultiCollection);
                break;

            // playlist is handled later
        }

        _mediaItems[content.Key] = items;

        var state = new CollectionEnumeratorState { Seed = context.Playout.Seed + index, Index = 0 };

        // marathon is a special case that needs to be handled on its own
        if (content is YamlPlayoutContentMarathonItem marathon)
        {
            if (!Enum.TryParse(marathon.ItemOrder, true, out PlaybackOrder playbackOrder))
            {
                playbackOrder = PlaybackOrder.Shuffle;
            }

            var allMediaItems = new List<MediaItem>();

            // grab items from each show guid
            foreach (string showGuid in marathon.Guids.Map(g => $"{g.Source}://{g.Value}"))
            {
                allMediaItems.AddRange(await mediaCollectionRepository.GetShowItemsByShowGuids([showGuid]));
            }

            // TODO: support different group_by

            var groups = allMediaItems
                .GroupBy(MediaItemKeyByShow)
                .ToList();

            Dictionary<PlaylistItem, List<MediaItem>> itemMap = [];

            foreach (IGrouping<int, MediaItem> group in groups)
            {
                PlaylistItem playlistItem = GroupToPlaylistItem(marathon, playbackOrder, group);
                itemMap.Add(playlistItem, group.ToList());
            }

            return await PlaylistEnumerator.Create(
                mediaCollectionRepository,
                itemMap,
                state,
                cancellationToken);
        }

        // playlist is a special case that needs to be handled on its own
        if (content is YamlPlayoutContentPlaylistItem playlist)
        {
            if (!string.IsNullOrWhiteSpace(playlist.Order))
            {
                logger.LogWarning(
                    "Ignoring playback order {Order} for playlist {Playlist}",
                    playlist.Order,
                    playlist.Playlist);
            }

            Dictionary<PlaylistItem, List<MediaItem>> itemMap =
                await mediaCollectionRepository.GetPlaylistItemMap(playlist.PlaylistGroup, playlist.Playlist);

            return await PlaylistEnumerator.Create(
                mediaCollectionRepository,
                itemMap,
                state,
                cancellationToken);
        }

        switch (Enum.Parse<PlaybackOrder>(content.Order, true))
        {
            case PlaybackOrder.Chronological:
                return new ChronologicalMediaCollectionEnumerator(items, state);
            case PlaybackOrder.Shuffle:
                bool keepMultiPartEpisodesTogether = content.MultiPart;
                List<GroupedMediaItem> groupedMediaItems = keepMultiPartEpisodesTogether
                    ? MultiPartEpisodeGrouper.GroupMediaItems(items, treatCollectionsAsShows: false)
                    : items.Map(mi => new GroupedMediaItem(mi, null)).ToList();
                return new BlockPlayoutShuffledMediaCollectionEnumerator(groupedMediaItems, state);
        }

        return Option<IMediaCollectionEnumerator>.None;
    }

    private int MediaItemKeyByShow(MediaItem mediaItem) =>
        mediaItem switch
        {
            Episode e => e.Season?.ShowId ?? 0,
            _ => 0
        };

    private static PlaylistItem GroupToPlaylistItem(
        YamlPlayoutContentMarathonItem marathon,
        PlaybackOrder playbackOrder,
        IGrouping<int, MediaItem> group) =>
        new()
        {
            Index = group.Key,

            CollectionType = ProgramScheduleItemCollectionType.TelevisionShow,
            MediaItemId = group.Key,

            PlayAll = marathon.PlayAllItems,
            PlaybackOrder = playbackOrder,

            IncludeInProgramGuide = true
        };
}
