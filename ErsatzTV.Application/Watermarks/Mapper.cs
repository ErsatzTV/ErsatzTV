﻿using ErsatzTV.Application.Artworks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Watermarks;

internal static class Mapper
{
    public static WatermarkViewModel ProjectToViewModel(ChannelWatermark watermark) =>
        new(
            watermark.Id,
            new ArtworkContentTypeModel(watermark.Image, watermark.OriginalContentType),
            watermark.Name,
            watermark.Mode,
            watermark.ImageSource,
            watermark.Location,
            watermark.Size,
            watermark.WidthPercent,
            watermark.HorizontalMarginPercent,
            watermark.VerticalMarginPercent,
            watermark.FrequencyMinutes,
            watermark.DurationSeconds,
            watermark.Opacity,
            watermark.PlaceWithinSourceContent);
}
