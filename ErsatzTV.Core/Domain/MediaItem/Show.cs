using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class Show : MediaItem
    {
        public List<Season> Seasons { get; set; }
        public List<ShowMetadata> ShowMetadata { get; set; }
    }
}
