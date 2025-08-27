namespace ErsatzTV.Core.Search;

public interface ISmartCollectionCache
{
    Task Refresh(CancellationToken cancellationToken);

    Task<bool> HasCycle(string name, CancellationToken cancellationToken);

    Task<Option<string>> GetQuery(string name, CancellationToken cancellationToken);
}
