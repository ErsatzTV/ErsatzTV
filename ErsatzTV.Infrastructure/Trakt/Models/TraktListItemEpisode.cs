namespace ErsatzTV.Infrastructure.Trakt.Models;

public class TraktListItemEpisode
{
    public int Season { get; set; }
    public int Number { get; set; }
    public string Title { get; set; }
    public TraktListItemIds Ids { get; set; }
}
