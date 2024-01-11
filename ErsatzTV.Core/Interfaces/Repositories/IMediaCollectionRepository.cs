using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaCollectionRepository
{
    Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id);
    Task<List<MediaItem>> GetItems(int id);
    Task<List<MediaItem>> GetMultiCollectionItems(int id);
    Task<List<MediaItem>> GetSmartCollectionItems(int id);
    Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id);
    Task<List<CollectionWithItems>> GetFakeMultiCollectionCollections(int? collectionId, int? smartCollectionId);
    Task<List<int>> PlayoutIdsUsingCollection(int collectionId);
    Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId);
    Task<List<int>> PlayoutIdsUsingSmartCollection(int smartCollectionId);
    Task<bool> IsCustomPlaybackOrder(int collectionId);
    Task<Option<string>> GetNameFromKey(CollectionKey emptyCollection);
    List<CollectionWithItems> GroupIntoFakeCollections(List<MediaItem> items, string fakeKey = null);
}
