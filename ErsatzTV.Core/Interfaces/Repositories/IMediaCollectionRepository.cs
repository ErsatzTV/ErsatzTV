using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaCollectionRepository
    {
        Task<Collection> Add(Collection collection);
        Task<Option<Collection>> Get(int id);
        Task<Option<Collection>> GetCollectionWithItems(int id);
        Task<Option<Collection>> GetCollectionWithItemsUntracked(int id);
        Task<List<Collection>> GetAll();
        Task<Option<List<MediaItem>>> GetItems(int id);
        Task Update(Collection collection);
        Task Delete(int collectionId);
    }
}
