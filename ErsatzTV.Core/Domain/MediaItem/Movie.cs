using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class Movie : MediaItem
    {
        public List<NewMovieMetadata> MovieMetadata { get; set; }

        public List<MediaVersion> MediaVersions { get; set; }

        // TODO: remove SimpleMediaCollections
        public List<SimpleMediaCollection> SimpleMediaCollections { get; set; }
    }
}
