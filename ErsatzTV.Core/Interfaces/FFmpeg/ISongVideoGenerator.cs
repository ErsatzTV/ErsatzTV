using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface ISongVideoGenerator
{
    Task<Tuple<string, MediaVersion>> GenerateSongVideo(
        Song song,
        Channel channel,
        Option<ChannelWatermark> maybePlayoutItemWatermark,
        Option<ChannelWatermark> maybeGlobalWatermark,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken);
}