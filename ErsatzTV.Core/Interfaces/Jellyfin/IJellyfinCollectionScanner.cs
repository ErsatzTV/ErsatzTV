namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinCollectionScanner
{
    Task<Either<BaseError, Unit>> ScanCollections(
        string address,
        string apiKey,
        int mediaSourceId);
}
