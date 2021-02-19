using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaItemRepository
    {
        public Task<int> Add(MediaItem mediaItem);
        public Task<Option<MediaItem>> Get(int id);
        public Task<List<MediaItem>> GetAll();
        public Task<List<MediaItem>> Search(string searchString);
        // public Task<List<MediaItemSummary>> GetPageByType(AggregateMediaItemType itemType, int pageNumber, int pageSize);
        // public Task<int> GetCountByType(AggregateMediaItemType itemType);
        public Task<List<MediaItem>> GetAllByMediaSourceId(int mediaSourceId);
        public Task<bool> Update(MediaItem mediaItem);
        public Task<Unit> Delete(int mediaItemId);
    }
}
