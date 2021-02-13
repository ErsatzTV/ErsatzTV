using System;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.Domain
{
    public record MediaMetadata : IDisplaySize
    {
        public TimeSpan Duration { get; set; }
        public string SampleAspectRatio { get; set; }
        public string DisplayAspectRatio { get; set; }
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        public MediaType MediaType { get; set; }
        public string Title { get; set; }
        public string SortTitle { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public string ContentRating { get; set; }
        public DateTime? Aired { get; set; }
        public VideoScanType VideoScanType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
