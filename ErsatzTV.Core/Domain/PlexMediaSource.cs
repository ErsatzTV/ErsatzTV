using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class PlexMediaSource : MediaSource
    {
        public PlexMediaSource() => SourceType = MediaSourceType.Plex;
        public string ProductVersion { get; set; }

        public string ClientIdentifier { get; set; }

        // public bool IsOwned { get; set; }
        public List<PlexMediaSourceConnection> Connections { get; set; }
        public List<PlexMediaSourceLibrary> Libraries { get; set; }
    }
}
