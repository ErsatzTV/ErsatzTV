namespace ErsatzTV.Core.Domain
{
    public class SeasonMetadata : Metadata
    {
        public string Outline { get; set; }
        public int SeasonId { get; set; }
        public Season Season { get; set; }
    }
}
