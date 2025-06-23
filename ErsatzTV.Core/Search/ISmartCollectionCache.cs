namespace ErsatzTV.Core.Search;

public interface ISmartCollectionCache
{
    Task Refresh();

    Task<bool> HasCycle(string name);

    Task<Option<string>> GetQuery(string name);
}
