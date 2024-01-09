using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalStatisticsProvider
{
    Task<Either<BaseError, MediaVersion>> GetStatistics(string ffmpegPath, string ffprobePath, string path);
    
    Task<Either<BaseError, bool>> RefreshStatistics(string ffmpegPath, string ffprobePath, MediaItem mediaItem);

    Task<Either<BaseError, Dictionary<string, string>>> GetSongTags(string ffprobePath, MediaItem mediaItem);
}
