namespace ErsatzTV.Infrastructure.Trakt.Models;

public class TraktListItemResponse
{
    public int Rank { get; set; }
    public int Id { get; set; }
    public string Type { get; set; }
    public TraktListItemMovie Movie { get; set; }
    public TraktListItemShow Show { get; set; }
    public TraktListItemSeason Season { get; set; }
    public TraktListItemEpisode Episode { get; set; }
}