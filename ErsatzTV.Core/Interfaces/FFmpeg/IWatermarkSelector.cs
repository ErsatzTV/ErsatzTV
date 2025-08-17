using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IWatermarkSelector
{
    Task<List<WatermarkOptions>> SelectWatermarks(
        Option<ChannelWatermark> globalWatermark,
        Channel channel,
        PlayoutItem playoutItem,
        DateTimeOffset now);

    WatermarkResult GetPlayoutItemWatermark(PlayoutItem playoutItem, DateTimeOffset now);

    Task<WatermarkOptions> GetWatermarkOptions(
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        Option<ChannelWatermark> watermarkOverride,
        Option<string> watermarkPath);
}
