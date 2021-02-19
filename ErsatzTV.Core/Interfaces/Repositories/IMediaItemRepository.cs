using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaItemRepository
    {
        Task<int> Add(MediaItem mediaItem);
        Task<Option<MediaItem>> Get(int id);
        Task<List<MediaItem>> GetAll();

        Task<List<MediaItem>> Search(string searchString);

        // Task<List<MediaItemSummary>> GetPageByType(AggregateMediaItemType itemType, int pageNumber, int pageSize);
        // Task<int> GetCountByType(AggregateMediaItemType itemType);
        Task<List<MediaItem>> GetAllByMediaSourceId(int mediaSourceId);
        Task<bool> Update(MediaItem mediaItem);
        Task<Unit> Delete(int mediaItemId);
    }
}
