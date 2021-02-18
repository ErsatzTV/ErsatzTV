using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMetadataProvider
    {
        Task<Unit> RefreshSidecarMetadata(MediaItem mediaItem, string path);
        Task<Unit> RefreshSidecarMetadata(TelevisionShow televisionShow, string path);
        Task<Unit> RefreshFallbackMetadata(MediaItem mediaItem);
        Task<Unit> RefreshFallbackMetadata(TelevisionShow televisionShow);
    }
}
