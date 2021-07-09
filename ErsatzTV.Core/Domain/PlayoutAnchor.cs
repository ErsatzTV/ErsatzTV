using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Domain
{
    public class PlayoutAnchor
    {
        public int NextScheduleItemId { get; set; }

        public ProgramScheduleItem NextScheduleItem { get; set; }

        public DateTime NextStart { get; set; }
        public int? MultipleRemaining { get; set; }
        public DateTime? DurationFinish { get; set; }
        public bool InFlood { get; set; }

        public DateTimeOffset NextStartOffset => new DateTimeOffset(NextStart, TimeSpan.Zero).ToLocalTime();

        public Option<DateTimeOffset> DurationFinishOffset =>
            Optional(DurationFinish)
                .Map(durationFinish => new DateTimeOffset(durationFinish, TimeSpan.Zero).ToLocalTime());
    }
}
