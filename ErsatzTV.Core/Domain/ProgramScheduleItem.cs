using System;

namespace ErsatzTV.Core.Domain
{
    public abstract class ProgramScheduleItem
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public StartType StartType => StartTime.HasValue ? StartType.Fixed : StartType.Dynamic;
        public TimeSpan? StartTime { get; set; }

        public ProgramScheduleItemCollectionType CollectionType { get; set; }

        // TODO: remove these
        public int? MediaCollectionId { get; set; }
        public MediaCollection MediaCollection { get; set; }
        public int? TelevisionShowId { get; set; }
        public TelevisionShow TelevisionShow { get; set; }
        public int? TelevisionSeasonId { get; set; }
        public TelevisionSeason TelevisionSeason { get; set; }
        public int ProgramScheduleId { get; set; }
        public ProgramSchedule ProgramSchedule { get; set; }

        public int? CollectionId { get; set; }
        public Collection Collection { get; set; }
        public int? MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
    }
}
