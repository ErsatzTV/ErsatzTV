using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCollections
{
    public record CollectionUpdateResult
    {
        public List<int> ModifiedPlayoutIds { get; set; }
    }
}
