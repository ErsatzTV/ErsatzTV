using ErsatzTV.Application.Artworks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Application.Watermarks;

public record CreateWatermark(
    string Name,
    ArtworkContentTypeModel Image,
    ChannelWatermarkMode Mode,
    ChannelWatermarkImageSource ImageSource,
    WatermarkLocation Location,
    WatermarkSize Size,
    int Width,
    int HorizontalMargin,
    int VerticalMargin,
    int FrequencyMinutes,
    int DurationSeconds,
    int Opacity,
    bool PlaceWithinSourceContent) : IRequest<Either<BaseError, CreateWatermarkResult>>;

public record CreateWatermarkResult(int WatermarkId) : EntityIdResult(WatermarkId);
