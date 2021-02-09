using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMetadataProvider
    {
        Task RefreshMetadata(MediaItem mediaItem);
    }
}
