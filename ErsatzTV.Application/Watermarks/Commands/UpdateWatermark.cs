﻿using ErsatzTV.Application.Artworks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Application.Watermarks;

public record UpdateWatermark(
    int Id,
    string Name,
    ArtworkContentTypeModel Image,
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
    bool PlaceWithinSourceContent) : IRequest<Either<BaseError, UpdateWatermarkResult>>;

public record UpdateWatermarkResult(int WatermarkId) : EntityIdResult(WatermarkId);
