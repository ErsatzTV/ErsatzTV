using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Commands
{
    public record UpdateChannel
    (
        int ChannelId,
        string Name,
        string Number,
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
        int WatermarkDurationSeconds,
        int WatermarkOpacity) : IRequest<Either<BaseError, ChannelViewModel>>;
}
