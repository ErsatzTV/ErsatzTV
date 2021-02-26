using System.Collections.Generic;

namespace ErsatzTV.Core.Plex
{
    public class PlexMediaEntry
    {
        public int Id { get; set; }
        public int Duration { get; set; }
        public int Bitrate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double AspectRatio { get; set; }
        public int AudioChannels { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }
        public string Container { get; set; }
        public string VideoFrameRate { get; set; }
        public List<PlexPartEntry> Part { get; set; }
    }
}
