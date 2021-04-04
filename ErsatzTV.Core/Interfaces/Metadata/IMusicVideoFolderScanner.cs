using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface IMusicVideoFolderScanner
    {
        Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            DateTimeOffset lastScan,
            decimal progressMin,
            decimal progressMax);
    }
}
