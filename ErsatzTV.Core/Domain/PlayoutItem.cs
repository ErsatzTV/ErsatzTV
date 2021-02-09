using System;

namespace ErsatzTV.Core.Domain
{
    public class PlayoutItem
    {
        public int Id { get; set; }
        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset Finish { get; set; }
        public int PlayoutId { get; set; }
        public Playout Playout { get; set; }
    }
}
