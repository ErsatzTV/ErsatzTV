using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.FFmpeg;

public record WatermarkOptions(
    ChannelWatermark Watermark,
    string ImagePath,
    Option<int> ImageStreamIndex)
{
    public static WatermarkOptions NoWatermark => new(null, null, None);
}
