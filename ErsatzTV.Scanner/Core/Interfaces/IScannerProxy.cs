namespace ErsatzTV.Scanner.Core.Interfaces;

public interface IScannerProxy
{
    void SetBaseUrl(string baseUrl);

    Task<bool> UpdateProgress(decimal progress, CancellationToken cancellationToken);
}
