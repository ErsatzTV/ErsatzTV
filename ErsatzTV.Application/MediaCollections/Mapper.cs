using ErsatzTV.Application.Tree;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

internal static class Mapper
{
    internal static MediaCollectionViewModel ProjectToViewModel(Collection collection) =>
        new(
            ProgramScheduleItemCollectionType.Collection,
            collection.Id,
            collection.Name,
            collection.UseCustomPlaybackOrder,
            MediaItemState.Normal);

    internal static MultiCollectionViewModel ProjectToViewModel(MultiCollection multiCollection) =>
        new(
            multiCollection.Id,
            multiCollection.Name,
            Optional(multiCollection.MultiCollectionItems).Flatten().Map(ProjectToViewModel).ToList(),
            Optional(multiCollection.MultiCollectionSmartItems).Flatten().Map(ProjectToViewModel).ToList());

    internal static SmartCollectionViewModel ProjectToViewModel(SmartCollection collection) =>
        new(collection.Id, collection.Name, collection.Query);

    internal static TraktListViewModel ProjectToViewModel(TraktList traktList) =>
        new(
            traktList.Id,
            traktList.TraktId,
            $"{traktList.User}/{traktList.List}",
            traktList.Name,
            traktList.ItemCount,
            traktList.Items.Count(i => i.MediaItemId.HasValue));

    private static MultiCollectionItemViewModel ProjectToViewModel(MultiCollectionItem multiCollectionItem) =>
        new(
            multiCollectionItem.MultiCollectionId,
            ProjectToViewModel(multiCollectionItem.Collection),
            multiCollectionItem.ScheduleAsGroup,
            multiCollectionItem.PlaybackOrder);

    private static MultiCollectionSmartItemViewModel ProjectToViewModel(
        MultiCollectionSmartItem multiCollectionSmartItem) =>
        new(
            multiCollectionSmartItem.MultiCollectionId,
            ProjectToViewModel(multiCollectionSmartItem.SmartCollection),
            multiCollectionSmartItem.ScheduleAsGroup,
            multiCollectionSmartItem.PlaybackOrder);

    internal static TreeViewModel ProjectToViewModel(List<PlaylistGroup> playlistGroups) =>
        new(
            playlistGroups.Map(bg => new TreeGroupViewModel(
                bg.Id,
                bg.Name,
                bg.Playlists.Map(b => new TreeItemViewModel(b.Id, b.Name)).ToList())).ToList());

    internal static PlaylistGroupViewModel ProjectToViewModel(PlaylistGroup playlistGroup) =>
        new(playlistGroup.Id, playlistGroup.Name, playlistGroup.Playlists.Count);

    internal static PlaylistViewModel ProjectToViewModel(Playlist playlist) =>
        new(playlist.Id, playlist.PlaylistGroupId, playlist.Name);

    internal static PlaylistItemViewModel ProjectToViewModel(PlaylistItem playlistItem) =>
        new(
            playlistItem.Id,
            playlistItem.Index,
            playlistItem.CollectionType,
            playlistItem.Collection is not null ? ProjectToViewModel(playlistItem.Collection) : null,
            playlistItem.MultiCollection is not null
                ? ProjectToViewModel(playlistItem.MultiCollection)
                : null,
            playlistItem.SmartCollection is not null
                ? ProjectToViewModel(playlistItem.SmartCollection)
                : null,
            playlistItem.MediaItem switch
            {
                Show show => MediaItems.Mapper.ProjectToViewModel(show),
                Season season => MediaItems.Mapper.ProjectToViewModel(season),
                Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                Movie movie => MediaItems.Mapper.ProjectToViewModel(movie),
                Episode episode => MediaItems.Mapper.ProjectToViewModel(episode),
                MusicVideo musicVideo => MediaItems.Mapper.ProjectToViewModel(musicVideo),
                OtherVideo otherVideo => MediaItems.Mapper.ProjectToViewModel(otherVideo),
                Song song => MediaItems.Mapper.ProjectToViewModel(song),
                Image image => MediaItems.Mapper.ProjectToViewModel(image),
                _ => null
            },
            playlistItem.PlaybackOrder,
            playlistItem.PlayAll,
            playlistItem.IncludeInProgramGuide);
}
