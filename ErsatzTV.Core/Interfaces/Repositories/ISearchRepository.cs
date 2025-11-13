using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ISearchRepository
{
    Task<Option<MediaItem>> GetItemToIndex(int id, CancellationToken cancellationToken);
    Task<List<string>> GetLanguagesForShow(Show show);
    Task<List<string>> GetSubLanguagesForShow(Show show);
    Task<List<string>> GetLanguagesForSeason(Season season);
    Task<List<string>> GetSubLanguagesForSeason(Season season);
    Task<List<string>> GetLanguagesForArtist(Artist artist);
    Task<List<string>> GetSubLanguagesForArtist(Artist artist);
    IAsyncEnumerable<MediaItem> GetAllMediaItems(CancellationToken cancellationToken);
}
