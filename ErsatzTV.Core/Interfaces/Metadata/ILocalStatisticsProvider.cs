using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalStatisticsProvider
{
    Task<Either<BaseError, MediaVersion>> GetStatistics(string ffprobePath, string path);

    Task<Either<BaseError, bool>> RefreshStatistics(string ffmpegPath, string ffprobePath, MediaItem mediaItem);

    Either<BaseError, List<SongTag>> GetSongTags(MediaItem mediaItem);

    Task<Option<double>> GetInterlacedRatio(
        string ffmpegPath,
        MediaItem mediaItem,
        CancellationToken cancellationToken);
}
