using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaCollectionRepository
    {
        public Task<SimpleMediaCollection> Add(SimpleMediaCollection collection);
        public Task<Option<MediaCollection>> Get(int id);
        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollection(int id);
        public Task<Option<TelevisionMediaCollection>> GetTelevisionMediaCollection(int id);
        public Task<List<SimpleMediaCollection>> GetSimpleMediaCollections();
        public Task<List<MediaCollection>> GetAll();
        public Task<List<MediaCollectionSummary>> GetSummaries(string searchString);
        public Task<Option<List<MediaItem>>> GetItems(int id);
        public Task<Option<List<MediaItem>>> GetSimpleMediaCollectionItems(int id);
        public Task<Option<List<MediaItem>>> GetTelevisionMediaCollectionItems(int id);
        public Task Update(SimpleMediaCollection collection);
        public Task InsertOrIgnore(TelevisionMediaCollection collection);
        public Task<Unit> ReplaceItems(int collectionId, List<MediaItem> mediaItems);
        public Task Delete(int mediaCollectionId);
        public Task DeleteEmptyTelevisionCollections();
    }
}
