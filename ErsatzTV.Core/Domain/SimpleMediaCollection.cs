using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class SimpleMediaCollection : MediaCollection
    {
        public IList<MediaItem> Items { get; set; }
    }
}
