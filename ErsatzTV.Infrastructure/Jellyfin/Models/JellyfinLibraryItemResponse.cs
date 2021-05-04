using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Jellyfin.Models
{
    public class JellyfinLibraryItemResponse
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Path { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Tags { get; set; }
        public int ProductionYear { get; set; }
        public string PremiereDate { get; set; }
        public List<JellyfinMediaStreamResponse> MediaStreams { get; set; }
    }
}
