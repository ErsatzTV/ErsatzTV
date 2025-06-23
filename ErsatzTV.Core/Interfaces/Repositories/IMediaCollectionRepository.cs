using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaCollectionRepository
{
    Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(int playlistId);
    Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(string groupName, string name);
    Task<Dictionary<PlaylistItem, List<MediaItem>>> GetPlaylistItemMap(Playlist playlist);
    Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id);
    Task<List<MediaItem>> GetItems(int id);
    Task<List<MediaItem>> GetCollectionItemsByName(string name);
    Task<List<MediaItem>> GetMultiCollectionItems(int id);
    Task<List<MediaItem>> GetMultiCollectionItemsByName(string name);
    Task<List<MediaItem>> GetSmartCollectionItems(int id);
    Task<List<MediaItem>> GetSmartCollectionItemsByName(string name);
    Task<List<MediaItem>> GetSmartCollectionItems(string query, string smartCollectionName);
    Task<List<MediaItem>> GetShowItemsByShowGuids(List<string> guids);
    Task<List<MediaItem>> GetPlaylistItems(int id);
    Task<List<Movie>> GetMovie(int id);
    Task<List<Episode>> GetEpisode(int id);
    Task<List<MusicVideo>> GetMusicVideo(int id);
    Task<List<OtherVideo>> GetOtherVideo(int id);
    Task<List<Song>> GetSong(int id);
    Task<List<Image>> GetImage(int id);
    Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id);
    Task<List<CollectionWithItems>> GetFakeMultiCollectionCollections(int? collectionId, int? smartCollectionId);
    Task<List<int>> PlayoutIdsUsingCollection(int collectionId);
    Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId);
    Task<List<int>> PlayoutIdsUsingSmartCollection(int smartCollectionId);
    Task<bool> IsCustomPlaybackOrder(int collectionId);
    Task<Option<string>> GetNameFromKey(CollectionKey emptyCollection);
    List<CollectionWithItems> GroupIntoFakeCollections(List<MediaItem> items, string fakeKey = null);
}
