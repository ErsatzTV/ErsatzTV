using ErsatzTV.Application.Artworks;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Application.Watermarks;

public record WatermarkViewModel(
    int Id,
    ArtworkContentTypeModel Image,
    string Name,
    ChannelWatermarkMode Mode,
    ChannelWatermarkImageSource ImageSource,
    WatermarkLocation Location,
    WatermarkSize Size,
    double Width,
    double HorizontalMargin,
    double VerticalMargin,
    int FrequencyMinutes,
    int DurationSeconds,
    int Opacity,
    bool PlaceWithinSourceContent
);
