using System;
using System.Diagnostics;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Core.Domain
{
    [DebuggerDisplay("{MediaItemId} - {Start} - {Finish}")]
    public class PlayoutItem
    {
        public int Id { get; set; }
        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public DateTime? GuideFinish { get; set; }
        public string CustomTitle { get; set; }
        public bool CustomGroup { get; set; }
        public FillerKind FillerKind { get; set; }
        public int PlayoutId { get; set; }
        public Playout Playout { get; set; }

        public DateTimeOffset StartOffset => new DateTimeOffset(Start, TimeSpan.Zero).ToLocalTime();
        public DateTimeOffset FinishOffset => new DateTimeOffset(Finish, TimeSpan.Zero).ToLocalTime();
        public DateTimeOffset? GuideFinishOffset => GuideFinish.HasValue
            ? new DateTimeOffset(GuideFinish.Value, TimeSpan.Zero).ToLocalTime()
            : null;
        
        // TODO: custom group => int for easy grouping, track on anchor?
    }
}
