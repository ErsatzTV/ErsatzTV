using System;

namespace ErsatzTV.Core.Domain
{
    public class ProgramScheduleItemDuration : ProgramScheduleItem
    {
        public TimeSpan PlayoutDuration { get; set; }
        public TailMode TailMode { get; set; }
        public ProgramScheduleItemCollectionType TailCollectionType { get; set; }
        public int? TailCollectionId { get; set; }
        public Collection TailCollection { get; set; }
        public int? TailMediaItemId { get; set; }
        public MediaItem TailMediaItem { get; set; }
        public int? TailMultiCollectionId { get; set; }
        public MultiCollection TailMultiCollection { get; set; }
        public int? TailSmartCollectionId { get; set; }
        public SmartCollection TailSmartCollection { get; set; }
    }
}
