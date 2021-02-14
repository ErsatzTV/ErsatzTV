using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalStatisticsProvider
    {
        Task<bool> RefreshStatistics(string ffprobePath, MediaItem mediaItem);
    }
}
