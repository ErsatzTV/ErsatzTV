using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ISearchRepository
    {
        public Task<List<MediaItem>> GetItemsToIndex();
        public Task<List<MediaItem>> SearchMediaItemsByTitle(string query);
        public Task<List<MediaItem>> SearchMediaItemsByGenre(string genre);
        public Task<List<MediaItem>> SearchMediaItemsByTag(string tag);
    }
}
