using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMetadataProvider
    {
        Task RefreshSidecarMetadata(MediaItem mediaItem, string path);
        Task RefreshFallbackMetadata(MediaItem mediaItem);
    }
}
