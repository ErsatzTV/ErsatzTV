namespace ErsatzTV.Core.Domain
{
    public class LocalMediaSource : MediaSource
    {
        public LocalMediaSource() => SourceType = MediaSourceType.Local;

        public MediaType MediaType { get; set; }
        public string Folder { get; set; }
    }
}
