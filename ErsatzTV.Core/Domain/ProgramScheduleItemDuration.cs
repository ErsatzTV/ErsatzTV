using System;

namespace ErsatzTV.Core.Domain
{
    public class ProgramScheduleItemDuration : ProgramScheduleItem
    {
        public TimeSpan PlayoutDuration { get; set; }
        public bool OfflineTail { get; set; }
    }
}
