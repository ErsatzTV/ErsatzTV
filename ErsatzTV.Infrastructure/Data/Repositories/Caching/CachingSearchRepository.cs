using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;

namespace ErsatzTV.Infrastructure.Data.Repositories.Caching;

public class CachingSearchRepository : ICachingSearchRepository
{
    private readonly ISearchRepository _searchRepository;
    private readonly SemaphoreSlim _slim = new(1, 1);
    private List<string> _allLanguageCodes;

    public CachingSearchRepository(ISearchRepository searchRepository) => _searchRepository = searchRepository;

    public Task<Option<MediaItem>> GetItemToIndex(int id) => _searchRepository.GetItemToIndex(id);

    public Task<List<string>> GetLanguagesForShow(Show show) => _searchRepository.GetLanguagesForShow(show);

    public Task<List<string>> GetLanguagesForSeason(Season season) => _searchRepository.GetLanguagesForSeason(season);

    public Task<List<string>> GetLanguagesForArtist(Artist artist) => _searchRepository.GetLanguagesForArtist(artist);

    public async Task<List<string>> GetAllLanguageCodes(List<string> mediaCodes)
    {
        if (_allLanguageCodes == null)
        {
            await _slim.WaitAsync();
            try
            {
                if (_allLanguageCodes == null)
                {
                    List<string> result = await _searchRepository.GetAllLanguageCodes(mediaCodes);
                    _allLanguageCodes = result;
                }
            }
            finally
            {
                _slim.Release();
            }
        }

        return _allLanguageCodes;
    }

    public IAsyncEnumerable<MediaItem> GetAllMediaItems() => _searchRepository.GetAllMediaItems();
}
