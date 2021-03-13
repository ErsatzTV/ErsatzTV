using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexPartResponse
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public int Duration { get; set; }
        public string File { get; set; }
        public List<PlexStreamResponse> Stream { get; set; }
    }
}
