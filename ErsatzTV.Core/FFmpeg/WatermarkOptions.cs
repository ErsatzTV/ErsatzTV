using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.FFmpeg;

public record WatermarkOptions(
    Option<ChannelWatermark> Watermark,
    Option<string> ImagePath,
    Option<int> ImageStreamIndex)
{
    public static WatermarkOptions NoWatermark => new(None, None, None);
}
