using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ISearchRepository
    {
        public Task<List<int>> GetItemIdsToIndex();
        public Task<Option<MediaItem>> GetItemToIndex(int id);
        public Task<List<MediaItem>> SearchMediaItemsByTitle(string query);
        public Task<List<MediaItem>> SearchMediaItemsByGenre(string genre);
        public Task<List<MediaItem>> SearchMediaItemsByTag(string tag);
        public Task<List<string>> GetLanguagesForShow(Show show);
    }
}
