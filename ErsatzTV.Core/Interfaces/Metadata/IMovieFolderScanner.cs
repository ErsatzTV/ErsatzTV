using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface IMovieFolderScanner
    {
        Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            decimal progressMin,
            decimal progressMax);
    }
}
