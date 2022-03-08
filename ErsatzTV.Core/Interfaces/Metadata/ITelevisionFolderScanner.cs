using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ITelevisionFolderScanner
{
    Task<Either<BaseError, Unit>> ScanFolder(
        LibraryPath libraryPath,
        string ffmpegPath,
        string ffprobePath,
        decimal progressMin,
        decimal progressMax);
}