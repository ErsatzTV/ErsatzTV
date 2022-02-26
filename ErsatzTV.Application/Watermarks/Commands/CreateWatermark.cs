using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Watermarks.Commands
{
    public record CreateWatermark(
        string Name,
        string Image,
        ChannelWatermarkMode Mode,
        ChannelWatermarkImageSource ImageSource,
        WatermarkLocation Location,
        WatermarkSize Size,
        int Width,
        int HorizontalMargin,
        int VerticalMargin,
        int FrequencyMinutes,
        int DurationSeconds,
        int Opacity) : IRequest<Either<BaseError, CreateWatermarkResult>>;

    public record CreateWatermarkResult(int WatermarkId) : EntityIdResult(WatermarkId);
}
