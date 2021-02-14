using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ISmartCollectionBuilder
    {
        Task<bool> RefreshSmartCollections(MediaItem mediaItem);
    }
}
