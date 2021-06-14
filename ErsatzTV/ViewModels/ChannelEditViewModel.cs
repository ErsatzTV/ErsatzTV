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
        public int? WatermarkId { get; set; }

        public UpdateChannel ToUpdate() =>
            new(
                Id,
                Name,
                Number,
                FFmpegProfileId,
                Logo,
                PreferredLanguageCode,
                StreamingMode,
                WatermarkId);

        public CreateChannel ToCreate() =>
            new(
                Name,
                Number,
                FFmpegProfileId,
                Logo,
                PreferredLanguageCode,
                StreamingMode,
                WatermarkId);
    }
}
