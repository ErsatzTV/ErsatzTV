using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
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
        Task<Option<TelevisionMediaCollection>> GetTelevisionMediaCollection(int id);
        Task<List<SimpleMediaCollection>> GetSimpleMediaCollections();
        Task<List<MediaCollection>> GetAll();
        Task<List<MediaCollectionSummary>> GetSummaries(string searchString);
        Task<Option<List<MediaItem>>> GetItems(int id);
        Task<Option<List<MediaItem>>> GetSimpleMediaCollectionItems(int id);
        Task<Option<List<MediaItem>>> GetTelevisionMediaCollectionItems(int id);
        Task Update(SimpleMediaCollection collection);
        Task<bool> InsertOrIgnore(TelevisionMediaCollection collection);
        Task<Unit> ReplaceItems(int collectionId, List<MediaItem> mediaItems);
        Task Delete(int mediaCollectionId);
        Task DeleteEmptyTelevisionCollections();
    }
}
