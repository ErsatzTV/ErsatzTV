namespace ErsatzTV.Core.Domain
{
    public class MultiCollectionSmartItem
    {
        public int MultiCollectionId { get; set; }
        public MultiCollection MultiCollection { get; set; }
        public int SmartCollectionId { get; set; }
        public SmartCollection SmartCollection { get; set; }
        public bool ScheduleAsGroup { get; set; }
        public PlaybackOrder PlaybackOrder { get; set; }
    }
}
