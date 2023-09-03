using System.Collections.Concurrent;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;

namespace ErsatzTV.Infrastructure.Data.Repositories.Caching;

public class CachingSearchRepository : ICachingSearchRepository
{
    private readonly ConcurrentDictionary<List<string>, List<string>> _cache = new();
    private readonly ISearchRepository _searchRepository;
    private readonly SemaphoreSlim _slim = new(1, 1);
    private bool _disposedValue;

    public CachingSearchRepository(ISearchRepository searchRepository) => _searchRepository = searchRepository;

    public Task<Option<MediaItem>> GetItemToIndex(int id) => _searchRepository.GetItemToIndex(id);

    public Task<List<string>> GetLanguagesForShow(Show show) => _searchRepository.GetLanguagesForShow(show);

    public Task<List<string>> GetLanguagesForSeason(Season season) => _searchRepository.GetLanguagesForSeason(season);

    public Task<List<string>> GetLanguagesForArtist(Artist artist) => _searchRepository.GetLanguagesForArtist(artist);

    public async Task<List<string>> GetAllLanguageCodes(List<string> mediaCodes)
    {
        if (!_cache.ContainsKey(mediaCodes))
        {
            await _slim.WaitAsync();
            try
            {
                _cache.TryAdd(mediaCodes, await _searchRepository.GetAllLanguageCodes(mediaCodes));
            }
            finally
            {
                _slim.Release();
            }
        }

        return _cache[mediaCodes];
    }

    public IAsyncEnumerable<MediaItem> GetAllMediaItems() => _searchRepository.GetAllMediaItems();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _slim.Dispose();
            }

            _disposedValue = true;
        }
    }
}
