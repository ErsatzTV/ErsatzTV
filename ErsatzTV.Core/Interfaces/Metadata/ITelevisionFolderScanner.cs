using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ITelevisionFolderScanner
    {
        Task<Unit> ScanFolder(LocalMediaSource localMediaSource, string ffprobePath);
    }
}
