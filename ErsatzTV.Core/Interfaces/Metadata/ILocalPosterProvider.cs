using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalPosterProvider
    {
        Task RefreshPoster(MediaItem mediaItem);
        Task SavePosterToDisk(MediaItem mediaItem, string posterPath);
    }
}
