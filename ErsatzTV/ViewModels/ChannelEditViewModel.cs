using ErsatzTV.Application.Channels.Commands;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class ChannelEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public int FFmpegProfileId { get; set; }
        public string PreferredLanguageCode { get; set; }
        public string Logo { get; set; }
        public StreamingMode StreamingMode { get; set; }
        public ChannelWatermarkMode WatermarkMode { get; set; }
        public ChannelWatermarkLocation WatermarkLocation { get; set; }
        public ChannelWatermarkSize WatermarkSize { get; set; }
        public int WatermarkWidth { get; set; }
        public int WatermarkHorizontalMargin { get; set; }
        public int WatermarkVerticalMargin { get; set; }
        public int WatermarkFrequencyMinutes { get; set; }
        public int WatermarkDurationSeconds { get; set; }
        public int WatermarkOpacity { get; set; }

        public UpdateChannel ToUpdate() =>
            new(
                Id,
                Name,
                Number,
                FFmpegProfileId,
                Logo,
                PreferredLanguageCode,
                StreamingMode,
                WatermarkMode,
                WatermarkLocation,
                WatermarkSize,
                WatermarkWidth,
                WatermarkHorizontalMargin,
                WatermarkVerticalMargin,
                WatermarkFrequencyMinutes,
                WatermarkDurationSeconds,
                WatermarkOpacity);

        public CreateChannel ToCreate() =>
            new(
                Name,
                Number,
                FFmpegProfileId,
                Logo,
                PreferredLanguageCode,
                StreamingMode,
                WatermarkMode,
                WatermarkLocation,
                WatermarkSize,
                WatermarkWidth,
                WatermarkHorizontalMargin,
                WatermarkVerticalMargin,
                WatermarkFrequencyMinutes,
                WatermarkDurationSeconds,
                WatermarkOpacity);
    }
}
