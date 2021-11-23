using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.FFmpeg
{
    public record WatermarkOptions(
        Option<ChannelWatermark> Watermark,
        Option<string> ImagePath,
        Option<int> ImageStreamIndex,
        bool IsAnimated);
}
