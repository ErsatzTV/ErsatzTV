using System;

namespace ErsatzTV.Core.Domain
{
    public class PlayoutItem
    {
        public int Id { get; set; }
        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public int PlayoutId { get; set; }
        public Playout Playout { get; set; }

        public DateTimeOffset StartOffset => new DateTimeOffset(Start, TimeSpan.Zero).ToLocalTime();
        public DateTimeOffset FinishOffset => new DateTimeOffset(Finish, TimeSpan.Zero).ToLocalTime();
    }
}
