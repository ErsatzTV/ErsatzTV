using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMetadataProvider
    {
        Task<ShowMetadata> GetMetadataForShow(string showFolder);
        Task<Unit> RefreshSidecarMetadata(MediaItem mediaItem, string path);
        Task<Unit> RefreshSidecarMetadata(Show televisionShow, string showFolder);
        Task<Unit> RefreshFallbackMetadata(MediaItem mediaItem);
        Task<Unit> RefreshFallbackMetadata(Show televisionShow, string showFolder);
    }
}
