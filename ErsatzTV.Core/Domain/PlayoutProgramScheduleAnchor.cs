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
        // TODO: remove this
        public int CollectionId { get; set; }
        // TODO: rename this
        public int? NewCollectionId { get; set; }
        public Collection Collection { get; set; }
        public int? MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
        public MediaCollectionEnumeratorState EnumeratorState { get; set; }
    }
}
