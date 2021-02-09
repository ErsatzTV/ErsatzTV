using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalStatisticsProvider
    {
        Task RefreshStatistics(string ffprobePath, MediaItem mediaItem);
    }
}
