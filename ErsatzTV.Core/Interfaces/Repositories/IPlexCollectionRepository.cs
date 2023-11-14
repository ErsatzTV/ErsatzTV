using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexCollectionRepository
{
    Task<List<PlexCollection>> GetCollections();
    Task<bool> AddCollection(PlexCollection collection);
    Task<bool> RemoveCollection(PlexCollection collection);
    Task<List<int>> RemoveAllTags(PlexCollection collection);
    Task<int> AddTag(MediaItem item, PlexCollection collection);
    Task<bool> SetEtag(PlexCollection collection);
}
