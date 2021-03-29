using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ITelevisionFolderScanner
    {
        Task<Either<BaseError, Unit>> ScanFolder(LibraryPath libraryPath, string ffprobePath, DateTimeOffset lastScan);
    }
}
