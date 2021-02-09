namespace ErsatzTV.Core.Domain
{
    public class PlexMediaSourceLibrary
    {
        public int Id { get; set; }
        public string Key { get; init; }
        public string Name { get; init; }
        public MediaType MediaType { get; init; }
    }
}
