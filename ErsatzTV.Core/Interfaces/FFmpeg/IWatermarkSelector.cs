using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IWatermarkSelector
{
    Task<WatermarkOptions> GetWatermarkOptions(
        string ffprobePath,
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        Option<ChannelWatermark> watermarkOverride,
        Option<string> watermarkPath);
}
