using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexResource
    {
        public string Name { get; set; }
        public string ProductVersion { get; set; }
        public string Platform { get; set; }
        public string PlatformVersion { get; set; }
        public string ClientIdentifier { get; set; }

        public string AccessToken { get; set; }

        public bool Owned { get; set; }
        public string Provides { get; set; }
        public bool HttpsRequired { get; set; }
        public List<PlexResourceConnection> Connections { get; set; }
    }
}
