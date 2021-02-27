using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class Season : MediaItem
    {
        public int SeasonNumber { get; set; }
        public int ShowId { get; set; }
        public Show Show { get; set; }

        public List<Episode> Episodes { get; set; }
        public List<SeasonMetadata> SeasonMetadata { get; set; }
    }
}
