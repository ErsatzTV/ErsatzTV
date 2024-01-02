using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Tests.Fakes;

public class FakeMediaCollectionRepository : IMediaCollectionRepository
{
    private readonly Map<int, List<MediaItem>> _data;

    public FakeMediaCollectionRepository(Map<int, List<MediaItem>> data) => _data = data;

    public Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id) =>
        throw new NotSupportedException();

    public Task<List<MediaItem>> GetItems(int id) => _data[id].ToList().AsTask();
    public Task<List<MediaItem>> GetMultiCollectionItems(int id) => throw new NotSupportedException();
    public Task<List<MediaItem>> GetSmartCollectionItems(int id) => _data[id].ToList().AsTask();

    public Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id) =>
        throw new NotSupportedException();

    public Task<List<CollectionWithItems>>
        GetFakeMultiCollectionCollections(int? collectionId, int? smartCollectionId) =>
        throw new NotSupportedException();

    public Task<List<int>> PlayoutIdsUsingCollection(int collectionId) => throw new NotSupportedException();

    public Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId) =>
        throw new NotSupportedException();

    public Task<List<int>> PlayoutIdsUsingSmartCollection(int smartCollectionId) =>
        throw new NotSupportedException();

    public Task<bool> IsCustomPlaybackOrder(int collectionId) => false.AsTask();
    public Task<Option<string>> GetNameFromKey(CollectionKey emptyCollection) => Option<string>.None.AsTask();

    public List<CollectionWithItems> GroupIntoFakeCollections(List<MediaItem> items) =>
        throw new NotSupportedException();
}
