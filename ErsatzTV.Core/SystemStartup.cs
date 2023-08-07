namespace ErsatzTV.Core;

public class SystemStartup
{
    private readonly SemaphoreSlim _databaseStartup = new(0, 100);
    private readonly SemaphoreSlim _searchIndexStartup = new(0, 100);

    public bool IsDatabaseReady { get; private set; }
    public bool IsSearchIndexReady { get; private set; }

    public event EventHandler OnDatabaseReady;
    public event EventHandler OnSearchIndexReady;

    public async Task WaitForDatabase(CancellationToken cancellationToken) =>
        await _databaseStartup.WaitAsync(cancellationToken);

    public async Task WaitForSearchIndex(CancellationToken cancellationToken) =>
        await _searchIndexStartup.WaitAsync(cancellationToken);

    public void DatabaseIsReady()
    {
        _databaseStartup.Release(100);
        IsDatabaseReady = true;
        OnDatabaseReady?.Invoke(this, EventArgs.Empty);
    }

    public void SearchIndexIsReady()
    {
        _searchIndexStartup.Release(100);
        IsSearchIndexReady = true;
        OnSearchIndexReady?.Invoke(this, EventArgs.Empty);
    }
}
