using System.Collections.Generic;

namespace ErsatzTV.Core.Domain;

public class TraktListItem
{
    public int Id { get; set; }
    public int TraktListId { get; set; }
    public TraktList TraktList { get; set; }

    public TraktListItemKind Kind { get; set; }
    public int TraktId { get; set; }
    public int Rank { get; set; }
    public string Title { get; set; }
    public int? Year { get; set; }
    public int? Season { get; set; }
    public int? Episode { get; set; }
    public List<TraktListItemGuid> Guids { get; set; }

    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }

    public string DisplayTitle => Kind switch
    {
        TraktListItemKind.Movie => $"{Title} ({Year})",
        TraktListItemKind.Show => $"{Title} ({Year})",
        TraktListItemKind.Season => $"{Title} ({Year}) S{Season:00}",
        _ => $"{Title} ({Year}) S{Season:00}E{Episode:00}"
    };
}