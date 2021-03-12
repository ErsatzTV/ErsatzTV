using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalStatisticsProvider
    {
        Task<Either<BaseError, Unit>> RefreshStatistics(string ffprobePath, MediaItem mediaItem);
    }
}
