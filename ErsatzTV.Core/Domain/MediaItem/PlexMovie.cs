namespace ErsatzTV.Core.Domain
{
    public class PlexMovie : Movie
    {
        public string Key { get; set; }
        public PlexMediaItemPart Part { get; set; }
    }
}
