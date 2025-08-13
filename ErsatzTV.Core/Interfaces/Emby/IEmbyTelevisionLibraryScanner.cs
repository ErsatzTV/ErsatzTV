using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyTelevisionLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);

    Task<Either<BaseError, Unit>> ScanSingleShow(
        string address,
        string apiKey,
        EmbyLibrary library,
        string showTitle,
        bool deepScan,
        CancellationToken cancellationToken);
}
