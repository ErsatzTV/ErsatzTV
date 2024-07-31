using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutMarathonHelper(IMediaCollectionRepository mediaCollectionRepository)
{
    public async Task<Option<IMediaCollectionEnumerator>> GetEnumerator(
        YamlPlayoutContentMarathonItem marathon,
        CollectionEnumeratorState state,
        CancellationToken cancellationToken)
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

        // grab items from each search
        foreach (string query in marathon.Searches)
        {
            allMediaItems.AddRange(await mediaCollectionRepository.GetSmartCollectionItems(query));
        }

        List<IGrouping<GroupKey, MediaItem>> groups = [];

        // group by show
        if (string.Equals(marathon.GroupBy, "show", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyByShow));
        }
        // group by season
        else if (string.Equals(marathon.GroupBy, "season", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyBySeason));
        }
        // group by artist
        else if (string.Equals(marathon.GroupBy, "artist", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyByArtist));
        }
        // group by album
        else if (string.Equals(marathon.GroupBy, "album", StringComparison.OrdinalIgnoreCase))
        {
            groups.AddRange(allMediaItems.GroupBy(MediaItemKeyByAlbum));
        }

        Dictionary<PlaylistItem, List<MediaItem>> itemMap = [];

        for (var index = 0; index < groups.Count; index++)
        {
            IGrouping<GroupKey, MediaItem> group = groups[index];
            PlaylistItem playlistItem = GroupToPlaylistItem(index, marathon, playbackOrder, group);
            itemMap.Add(playlistItem, group.ToList());
        }

        return await PlaylistEnumerator.Create(
            mediaCollectionRepository,
            itemMap,
            state,
            cancellationToken);
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
        YamlPlayoutContentMarathonItem marathon,
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

            PlayAll = marathon.PlayAllItems,
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
