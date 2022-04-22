namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyCollectionScanner
{
    Task<Either<BaseError, Unit>> ScanCollections(
        string address,
        string apiKey);
}
