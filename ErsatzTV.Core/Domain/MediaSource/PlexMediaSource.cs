using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class PlexMediaSource : MediaSource
    {
        public string ServerName { get; set; }
        public string ProductVersion { get; set; }
        public string ClientIdentifier { get; set; }

        // public bool IsOwned { get; set; }
        public List<PlexConnection> Connections { get; set; }
        public List<PlexPathReplacement> PathReplacements { get; set; }
    }
}
