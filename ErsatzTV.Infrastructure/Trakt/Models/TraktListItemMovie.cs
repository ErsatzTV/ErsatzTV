namespace ErsatzTV.Infrastructure.Trakt.Models
{
    public class TraktListItemMovie
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public TraktListItemIds Ids { get; set; }
    }
}
