namespace ErsatzTV.Core.Domain
{
    public class JellyfinPathReplacement
    {
        public int Id { get; set; }
        public string JellyfinPath { get; set; }
        public string LocalPath { get; set; }
        public int JellyfinMediaSourceId { get; set; }
        public JellyfinMediaSource JellyfinMediaSource { get; set; }
    }
}
