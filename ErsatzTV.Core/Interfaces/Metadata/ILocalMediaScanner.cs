using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMediaScanner
    {
        Task<Unit> ScanMovies(LocalMediaSource localMediaSource, string ffprobePath);
        Task<Unit> ScanTelevision(LocalMediaSource localMediaSource, string ffprobePath);
    }
}
