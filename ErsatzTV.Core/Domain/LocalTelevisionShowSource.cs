namespace ErsatzTV.Core.Domain
{
    public class LocalTelevisionShowSource : TelevisionShowSource
    {
        public int MediaSourceId { get; set; }
        public LocalMediaSource MediaSource { get; set; }
        public string Path { get; set; }
    }
}
