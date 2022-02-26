using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Application.Watermarks
{
    public record WatermarkViewModel(
        int Id,
        string Image,
        string Name,
        ChannelWatermarkMode Mode,
        ChannelWatermarkImageSource ImageSource,
        WatermarkLocation Location,
        WatermarkSize Size,
        int Width,
        int HorizontalMargin,
        int VerticalMargin,
        int FrequencyMinutes,
        int DurationSeconds,
        int Opacity
    );
}
