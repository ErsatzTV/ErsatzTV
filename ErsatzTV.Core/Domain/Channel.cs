using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Core.Domain
{
    public class Channel
    {
        public static string NumberValidator = @"^[0-9]+(\.[0-9])?$";

        public Channel(Guid uniqueId) => UniqueId = uniqueId;
        public int Id { get; set; }
        public Guid UniqueId { get; init; }
        public string Number { get; set; }
        public string Name { get; set; }
        public int FFmpegProfileId { get; set; }
        public FFmpegProfile FFmpegProfile { get; set; }
        public int? WatermarkId { get; set; }
        public ChannelWatermark Watermark { get; set; }
        public int? FallbackFillerId { get; set; }
        public FillerPreset FallbackFiller { get; set; }
        public StreamingMode StreamingMode { get; set; }
        public List<Playout> Playouts { get; set; }
        public List<Artwork> Artwork { get; set; }
        public string PreferredLanguageCode { get; set; }
    }
}
