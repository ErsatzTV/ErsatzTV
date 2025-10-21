namespace ErsatzTV.Scanner.Core.Interfaces;

public interface IScannerProxy
{
    void SetBaseUrl(string baseUrl);

    Task<bool> UpdateProgress(decimal progress, CancellationToken cancellationToken);

    Task<bool> ReindexMediaItems(int[] mediaItemIds, CancellationToken cancellationToken);

    Task<bool> RemoveMediaItems(int[] mediaItemIds, CancellationToken cancellationToken);
}
