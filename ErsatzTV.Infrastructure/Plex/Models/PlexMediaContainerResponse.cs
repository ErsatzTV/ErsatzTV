using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexMediaContainerResponse<T>
    {
        public T MediaContainer { get; set; }
    }

    public class PlexMediaContainerDirectoryContent<T>
    {
        public List<T> Directory { get; set; }
    }

    public class PlexMediaContainerMetadataContent<T>
    {
        public List<T> Metadata { get; set; }
    }
}
