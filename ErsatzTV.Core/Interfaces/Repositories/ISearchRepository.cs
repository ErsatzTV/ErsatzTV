using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ISearchRepository
{
    Task<Option<MediaItem>> GetItemToIndex(int id);
    Task<List<string>> GetLanguagesForShow(Show show);
    Task<List<string>> GetLanguagesForSeason(Season season);
    Task<List<string>> GetLanguagesForArtist(Artist artist);
    Task<List<string>> GetAllLanguageCodes(List<string> mediaCodes);
    IAsyncEnumerable<MediaItem> GetAllMediaItems();
}
