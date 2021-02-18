using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalPosterProvider
    {
        public Task RefreshPoster(MediaItem mediaItem);
        public Task SavePosterToDisk(MediaItem mediaItem, string posterPath);
    }
}
