using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaCollectionRepository
    {
        Task<Collection> Add(Collection collection);
        Task<bool> AddMediaItem(int collectionId, int mediaItemId);
        Task<bool> AddMediaItems(int collectionId, List<int> mediaItemIds);
        Task<Option<Collection>> Get(int id);
        Task<Option<Collection>> GetCollectionWithItems(int id);
        Task<Option<Collection>> GetCollectionWithItemsUntracked(int id);
        Task<List<Collection>> GetAll();
        Task<Option<List<MediaItem>>> GetItems(int id);
        Task<bool> Update(Collection collection);
        Task Delete(int collectionId);
        Task<List<int>> PlayoutIdsUsingCollection(int collectionId);
    }
}
