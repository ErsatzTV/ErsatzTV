namespace ErsatzTV.Core.Domain
{
    public class PlexMediaItemPart
    {
        public int Id { get; set; }
        public int PlexId { get; set; }
        public string Key { get; set; }
        public int Duration { get; set; }
        public string File { get; set; }
        public int Size { get; set; }
    }
}
