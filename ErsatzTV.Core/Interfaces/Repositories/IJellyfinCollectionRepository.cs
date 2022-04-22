using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IJellyfinCollectionRepository
{
    Task<List<JellyfinCollection>> GetCollections();
    Task<bool> AddCollection(JellyfinCollection collection);
    Task<bool> RemoveCollection(JellyfinCollection collection);
    Task<List<int>> RemoveAllTags(JellyfinCollection collection);
    Task<int> AddTag(MediaItem item, JellyfinCollection collection);
    Task<bool> SetEtag(JellyfinCollection collection);
}
