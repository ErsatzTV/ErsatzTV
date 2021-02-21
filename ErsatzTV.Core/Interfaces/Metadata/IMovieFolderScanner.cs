using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface IMovieFolderScanner
    {
        Task<Either<BaseError, Unit>> ScanFolder(LocalMediaSource localMediaSource, string ffprobePath);
    }
}
