using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class JellyfinMediaSource : MediaSource
    {
        public string ServerName { get; set; }
        public string OperatingSystem { get; set; }
        public List<JellyfinConnection> Connections { get; set; }
        public List<JellyfinPathReplacement> PathReplacements { get; set; }
    }
}
