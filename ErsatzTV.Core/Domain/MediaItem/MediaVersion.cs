using System;
using System.Collections.Generic;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.Domain
{
    public class MediaVersion : IDisplaySize
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<MediaFile> MediaFiles { get; set; }

        public TimeSpan Duration { get; set; }
        public string SampleAspectRatio { get; set; }
        public string DisplayAspectRatio { get; set; }
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        public VideoScanKind VideoScanKind { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
