using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Watermarks
{
    public record WatermarkViewModel(
        int Id,
        string Image,
        string Name,
        ChannelWatermarkMode Mode,
        ChannelWatermarkImageSource ImageSource,
        ChannelWatermarkLocation Location,
        ChannelWatermarkSize Size,
        int Width,
        int HorizontalMargin,
        int VerticalMargin,
        int FrequencyMinutes,
        int DurationSeconds,
        int Opacity
    );
}
