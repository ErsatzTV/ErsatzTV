using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalStatisticsProvider
{
    Task<Either<BaseError, bool>> RefreshStatistics(string ffmpegPath, string ffprobePath, MediaItem mediaItem);

    Task<Either<BaseError, bool>> RefreshStatistics(
        string ffmpegPath,
        string ffprobePath,
        MediaItem mediaItem,
        string mediaItemPath);

    Task<Either<BaseError, Dictionary<string, string>>> GetSongTags(string ffprobePath, MediaItem mediaItem);
}
