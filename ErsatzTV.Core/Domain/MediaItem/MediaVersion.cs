using System;
using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class MediaVersion
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<MediaFile> MediaFiles { get; set; }

        public TimeSpan Duration { get; set; }
        public string SampleAspectRatio { get; set; }
        public string DisplayAspectRatio { get; set; }
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        public bool IsInterlaced { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
