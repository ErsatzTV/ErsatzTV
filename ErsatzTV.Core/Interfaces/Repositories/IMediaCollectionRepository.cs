using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaCollectionRepository
    {
        Task<SimpleMediaCollection> Add(SimpleMediaCollection collection);
        Task<Option<MediaCollection>> Get(int id);
        Task<Option<SimpleMediaCollection>> GetSimpleMediaCollection(int id);
        Task<Option<SimpleMediaCollection>> GetSimpleMediaCollectionWithItems(int id);
        Task<Option<SimpleMediaCollection>> GetSimpleMediaCollectionWithItemsUntracked(int id);
        Task<List<SimpleMediaCollection>> GetSimpleMediaCollections();
        Task<List<MediaCollection>> GetAll();
        Task<Option<List<MediaItem>>> GetItems(int id);
        Task<Option<List<MediaItem>>> GetSimpleMediaCollectionItems(int id);
        Task Update(SimpleMediaCollection collection);
        Task Delete(int mediaCollectionId);
    }
}
