using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ISearchRepository
    {
        Task<List<int>> GetItemIdsToIndex();
        Task<Option<MediaItem>> GetItemToIndex(int id);
        Task<List<string>> GetLanguagesForShow(Show show);
        Task<List<string>> GetLanguagesForArtist(Artist artist);
        Task<List<string>> GetAllLanguageCodes(List<string> mediaCodes);
    }
}
