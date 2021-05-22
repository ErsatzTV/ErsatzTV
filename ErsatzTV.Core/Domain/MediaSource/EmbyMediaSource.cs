using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class EmbyMediaSource : MediaSource
    {
        public string ServerName { get; set; }
        public string OperatingSystem { get; set; }
        public List<EmbyConnection> Connections { get; set; }
        public List<EmbyPathReplacement> PathReplacements { get; set; }
    }
}
