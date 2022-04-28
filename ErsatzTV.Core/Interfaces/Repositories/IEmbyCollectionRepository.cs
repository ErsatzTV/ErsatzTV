using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IEmbyCollectionRepository
{
    Task<List<EmbyCollection>> GetCollections();
    Task<bool> AddCollection(EmbyCollection collection);
    Task<bool> RemoveCollection(EmbyCollection collection);
    Task<List<int>> RemoveAllTags(EmbyCollection collection);
    Task<int> AddTag(MediaItem item, EmbyCollection collection);
    Task<bool> SetEtag(EmbyCollection collection);
}
