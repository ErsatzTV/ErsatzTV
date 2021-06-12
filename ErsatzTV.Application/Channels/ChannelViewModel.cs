using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels
{
    public record ChannelViewModel(
        int Id,
        string Number,
        string Name,
        int FFmpegProfileId,
        string Logo,
        string PreferredLanguageCode,
        StreamingMode StreamingMode,
        ChannelWatermarkMode WatermarkMode,
        ChannelWatermarkLocation WatermarkLocation,
        ChannelWatermarkSize WatermarkSize,
        int WatermarkWidth,
        int WatermarkHorizontalMargin,
        int WatermarkVerticalMargin,
        int WatermarkFrequencyMinutes,
        int WatermarkDurationSeconds);
}
