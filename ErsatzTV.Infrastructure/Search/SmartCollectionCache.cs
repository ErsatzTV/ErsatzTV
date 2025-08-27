using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Search;

public sealed class SmartCollectionCache(IDbContextFactory<TvContext> dbContextFactory)
    : ISmartCollectionCache, IDisposable
{
    private readonly Dictionary<string, SmartCollectionData> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly AdjGraph _graph = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public void Dispose() => _semaphoreSlim.Dispose();

    public async Task Refresh(CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            List<SmartCollection> smartCollections = await dbContext.SmartCollections
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _data.Clear();
            _graph.Clear();

            foreach (SmartCollection smartCollection in smartCollections)
            {
                var data = new SmartCollectionData(smartCollection.Query);
                _data.Add(smartCollection.Name, data);

                foreach (Match match in SearchQueryParser.SmartCollectionRegex().Matches(smartCollection.Query))
                {
                    string otherCollectionName = match.Groups[1].Value;
                    _graph.AddEdge(smartCollection.Name, otherCollectionName);
                }
            }

            foreach (SmartCollection smartCollection in smartCollections)
            {
                SmartCollectionData data = _data[smartCollection.Name];
                data.HasCycle = _graph.HasCycle(smartCollection.Name);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<bool> HasCycle(string name, CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            return _data.TryGetValue(name, out SmartCollectionData data) && data.HasCycle;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<Option<string>> GetQuery(string name, CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            return _data.TryGetValue(name, out SmartCollectionData data) ? data.Query : Option<string>.None;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private record SmartCollectionData(string Query)
    {
        public bool HasCycle { get; set; }
    }
}
