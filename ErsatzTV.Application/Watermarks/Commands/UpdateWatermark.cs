using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Application.Watermarks;

public record UpdateWatermark(
    int Id,
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
    int Opacity) : IRequest<Either<BaseError, UpdateWatermarkResult>>;

public record UpdateWatermarkResult(int WatermarkId) : EntityIdResult(WatermarkId);
