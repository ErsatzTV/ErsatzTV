namespace ErsatzTV.Core.Domain
{
    public class EmbyConnection
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public int EmbyMediaSourceId { get; set; }
        public EmbyMediaSource EmbyMediaSource { get; set; }
    }
}
