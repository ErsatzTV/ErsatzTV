namespace ErsatzTV.Core.Interfaces.Metadata;

public interface IScannerProxyService
{
    Option<Guid> StartScan(int libraryId);
    void EndScan(Guid scanId);
    Task Progress(Guid scanId, decimal percentComplete);
    bool IsActive(Guid scanId);
    Option<decimal> GetProgress(int libraryId);
}
