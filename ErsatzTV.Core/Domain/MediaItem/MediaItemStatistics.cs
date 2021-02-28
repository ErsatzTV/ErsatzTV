using System;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.Domain
{
    public record MediaItemStatistics : IDisplaySize
    {
        public DateTime? LastWriteTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string SampleAspectRatio { get; set; }
        public string DisplayAspectRatio { get; set; }
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        public VideoScanKind VideoScanType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
