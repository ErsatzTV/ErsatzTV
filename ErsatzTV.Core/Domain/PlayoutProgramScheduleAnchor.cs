namespace ErsatzTV.Core.Domain
{
    public class PlayoutProgramScheduleAnchor
    {
        public int PlayoutId { get; set; }
        public Playout Playout { get; set; }
        public int ProgramScheduleId { get; set; }
        public ProgramSchedule ProgramSchedule { get; set; }
        public int MediaCollectionId { get; set; }
        public MediaCollection MediaCollection { get; set; }
        public MediaCollectionEnumeratorState EnumeratorState { get; set; }
    }
}
