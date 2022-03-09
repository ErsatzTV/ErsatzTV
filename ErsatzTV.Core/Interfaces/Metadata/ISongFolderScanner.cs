using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ISongFolderScanner
{
    Task<Either<BaseError, Unit>> ScanFolder(
        LibraryPath libraryPath,
        string ffprobePath,
        string ffmpegPath,
        decimal progressMin,
        decimal progressMax,
        CancellationToken cancellationToken);
}