using System;

namespace ErsatzTV.Core.Domain
{
    public class PlayoutAnchor
    {
        public int NextScheduleItemId { get; set; }

        public ProgramScheduleItem NextScheduleItem { get; set; }

        public DateTime NextStart { get; set; }

        public DateTimeOffset NextStartOffset => new DateTimeOffset(NextStart, TimeSpan.Zero).ToLocalTime();
    }
}
