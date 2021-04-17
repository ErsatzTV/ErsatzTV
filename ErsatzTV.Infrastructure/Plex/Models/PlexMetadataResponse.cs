using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexMetadataResponse
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public int Year { get; set; }
        public string Tagline { get; set; }
        public string Thumb { get; set; }
        public string Art { get; set; }
        public string OriginallyAvailableAt { get; set; }
        public long AddedAt { get; set; }
        public long UpdatedAt { get; set; }
        public int Index { get; set; }
        public string Studio { get; set; }
        public List<PlexMediaResponse> Media { get; set; }
        public List<PlexGenreResponse> Genre { get; set; }
        public List<PlexRoleResponse> Role { get; set; }
    }
}
