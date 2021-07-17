namespace ErsatzTV.Core.Domain
{
    public class PlayoutProgramScheduleAnchor
    {
        public int Id { get; set; }
        public int PlayoutId { get; set; }
        public Playout Playout { get; set; }
        public int ProgramScheduleId { get; set; }
        public ProgramSchedule ProgramSchedule { get; set; }
        public ProgramScheduleItemCollectionType CollectionType { get; set; }
        public int? CollectionId { get; set; }
        public Collection Collection { get; set; }
        public int? MultiCollectionId { get; set; }
        public MultiCollection MultiCollection { get; set; }
        public int? MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
        public CollectionEnumeratorState EnumeratorState { get; set; }
    }
}
