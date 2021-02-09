using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexMediaContainerResponse<T>
    {
        public PlexMediaContainerContent<T> MediaContainer { get; set; }
    }

    public class PlexMediaContainerContent<T>
    {
        public List<T> Directory { get; set; }
    }
}
