namespace ErsatzTV.Core;

public class SystemStartup
{
    private readonly SemaphoreSlim _databaseStartup = new(0, 100);

    public event EventHandler OnDatabaseReady;
    
    public bool IsDatabaseReady { get; private set; }

    public async Task WaitForDatabase(CancellationToken cancellationToken) =>
        await _databaseStartup.WaitAsync(cancellationToken);
    
    public void DatabaseIsReady()
    {
        _databaseStartup.Release(100);
        IsDatabaseReady = true;
        OnDatabaseReady?.Invoke(this, EventArgs.Empty);
    }
}
