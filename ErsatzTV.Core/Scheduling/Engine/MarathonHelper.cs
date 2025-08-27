using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.Scheduling.Engine;

public class MarathonHelper(IMediaCollectionRepository mediaCollectionRepository)
{
    public async Task<Option<MarathonContentResult>> GetEnumerator(
        Dictionary<string, List<string>> guids,
        List<string> searches,
        string groupBy,
        bool shuffleGroups,
        PlaybackOrder itemPlaybackOrder,
        bool playAllItems,
        CollectionEnumeratorState state,
        CancellationToken cancellationToken)
    {
        var allMediaItems = new List<MediaItem>();

        // grab items from each show guid
        foreach (string showGuid in guids.SelectMany(g => g.Value.Select(v => $"{g.Key}://{v}")))
        {
            allMediaItems.AddRange(await mediaCollectionRepository.GetShowItemsByShowGuids([showGuid]));
        }

        // grab items from each search
        foreach (string query in searches)
        {
            allMediaItems.AddRange(await mediaCollectionRepository.GetSmartCollectionItems(query, string.Empty, cancellationToken));
        }

        List<IGrouping<GroupKey, MediaItem>> groups = [];

        // group by show
        if (string.Equals(groupBy, "show", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyByShow));
        }
        // group by season
        else if (string.Equals(groupBy, "season", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyBySeason));
        }
        // group by artist
        else if (string.Equals(groupBy, "artist", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyByArtist));
        }
        // group by album
        else if (string.Equals(groupBy, "album", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyByAlbum));
        }

        Dictionary<PlaylistItem, List<MediaItem>> itemMap = [];

        for (var index = 0; index < groups.Count; index++)
        {
            IGrouping<GroupKey, MediaItem> group = groups[index];
            PlaylistItem playlistItem = GroupToPlaylistItem(index, playAllItems, itemPlaybackOrder, group);
            itemMap.Add(playlistItem, group.ToList());
        }

        PlaylistEnumerator enumerator = await PlaylistEnumerator.Create(
            mediaCollectionRepository,
            itemMap,
            state,
            shuffleGroups,
            cancellationToken);

        return new MarathonContentResult(
            enumerator,
            itemMap.ToImmutableDictionary(x => CollectionKey.ForPlaylistItem(x.Key), x => x.Value));
    }

    private static GroupKey MediaItemKeyByShow(MediaItem mediaItem) =>
        mediaItem switch
        {
            Episode e => new GroupKey(
                ProgramScheduleItemCollectionType.TelevisionShow,
                null,
                null,
                null,
                e.Season?.ShowId ?? 0),
            _ => new GroupKey(ProgramScheduleItemCollectionType.TelevisionShow, null, null, null, 0)
        };

    private static GroupKey MediaItemKeyBySeason(MediaItem mediaItem) =>
        mediaItem switch
        {
            Episode e => new GroupKey(
                ProgramScheduleItemCollectionType.TelevisionSeason,
                null,
                null,
                null,
                e.SeasonId),
            _ => new GroupKey(ProgramScheduleItemCollectionType.TelevisionSeason, null, null, null, 0)
        };

    private static GroupKey MediaItemKeyByArtist(MediaItem mediaItem) =>
        mediaItem switch
        {
            MusicVideo mv => new GroupKey(
                ProgramScheduleItemCollectionType.Artist,
                null,
                null,
                null,
                mv.ArtistId),
            _ => new GroupKey(ProgramScheduleItemCollectionType.Artist, null, null, null, 0)
        };

    private static GroupKey MediaItemKeyByAlbum(MediaItem mediaItem) =>
        mediaItem switch
        {
            Song s => new GroupKey(
                ProgramScheduleItemCollectionType.Collection,
                s.SongMetadata.HeadOrNone().Map(sm => sm.Album.GetStableHashCode()).IfNone(0),
                null,
                null,
                null),
            MusicVideo mv => new GroupKey(
                ProgramScheduleItemCollectionType.Collection,
                mv.MusicVideoMetadata.HeadOrNone()
                    .Map(mvm => $"{mv.ArtistId}-${mvm.Album}".GetStableHashCode()).IfNone(0),
                null,
                null,
                null),
            _ => new GroupKey(ProgramScheduleItemCollectionType.Collection, 0, null, null, null)
        };

    private static PlaylistItem GroupToPlaylistItem(
        int index,
        bool playAllItems,
        PlaybackOrder playbackOrder,
        IGrouping<GroupKey, MediaItem> group) =>
        new()
        {
            Index = index,

            CollectionType = group.Key.CollectionType,
            CollectionId = group.Key.CollectionId,
            MultiCollectionId = group.Key.MultiCollectionId,
            SmartCollectionId = group.Key.SmartCollectionId,
            MediaItemId = group.Key.MediaItemId,

            PlayAll = playAllItems,
            PlaybackOrder = playbackOrder,

            IncludeInProgramGuide = true
        };

    private record GroupKey(
        ProgramScheduleItemCollectionType CollectionType,
        int? CollectionId,
        int? MultiCollectionId,
        int? SmartCollectionId,
        int? MediaItemId);
}
