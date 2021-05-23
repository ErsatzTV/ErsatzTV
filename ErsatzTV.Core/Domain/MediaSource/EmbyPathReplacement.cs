namespace ErsatzTV.Core.Domain
{
    public class EmbyPathReplacement
    {
        public int Id { get; set; }
        public string EmbyPath { get; set; }
        public string LocalPath { get; set; }
        public int EmbyMediaSourceId { get; set; }
        public EmbyMediaSource EmbyMediaSource { get; set; }
    }
}
