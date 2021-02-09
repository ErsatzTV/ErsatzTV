using System;

namespace ErsatzTV.Core.Domain
{
    public class PlayoutAnchor
    {
        public int NextScheduleItemId { get; set; }

        public ProgramScheduleItem NextScheduleItem { get; set; }

        public DateTimeOffset NextStart { get; set; }
    }
}
