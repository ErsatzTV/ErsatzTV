﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaCollectionRepository
{
    Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(
        int playlistId,
        CancellationToken cancellationToken);

    Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(
        string groupName,
        string name,
        CancellationToken cancellationToken);

    Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(
        Playlist playlist,
        CancellationToken cancellationToken);

    Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id);
    Task<List<MediaItem>> GetItems(int id);
    Task<List<MediaItem>> GetCollectionItemsByName(string name, CancellationToken cancellationToken);
    Task<List<MediaItem>> GetMultiCollectionItems(int id, CancellationToken cancellationToken);
    Task<List<MediaItem>> GetMultiCollectionItemsByName(string name, CancellationToken cancellationToken);
    Task<List<MediaItem>> GetSmartCollectionItems(int id, CancellationToken cancellationToken);
    Task<List<MediaItem>> GetSmartCollectionItemsByName(string name, CancellationToken cancellationToken);
    Task<List<MediaItem>> GetSmartCollectionItems(string query, string smartCollectionName);
    Task<List<MediaItem>> GetShowItemsByShowGuids(List<string> guids);
    Task<List<MediaItem>> GetPlaylistItems(int id, CancellationToken cancellationToken);
    Task<List<Movie>> GetMovie(int id);
    Task<List<Episode>> GetEpisode(int id);
    Task<List<MusicVideo>> GetMusicVideo(int id);
    Task<List<OtherVideo>> GetOtherVideo(int id);
    Task<List<Song>> GetSong(int id);
    Task<List<Image>> GetImage(int id);
    Task<List<RemoteStream>> GetRemoteStream(int id);
    Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id, CancellationToken cancellationToken);

    Task<List<CollectionWithItems>> GetFakeMultiCollectionCollections(
        int? collectionId,
        int? smartCollectionId,
        CancellationToken cancellationToken);

    Task<List<int>> PlayoutIdsUsingCollection(int collectionId);
    Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId);
    Task<List<int>> PlayoutIdsUsingSmartCollection(int smartCollectionId);
    Task<bool> IsCustomPlaybackOrder(int collectionId);
    Task<Option<string>> GetNameFromKey(CollectionKey emptyCollection, CancellationToken cancellationToken);
    List<CollectionWithItems> GroupIntoFakeCollections(List<MediaItem> items, string fakeKey = null);
}
