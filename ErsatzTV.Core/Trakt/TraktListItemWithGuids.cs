using System.Collections.Generic;

namespace ErsatzTV.Core.Trakt
{
    public record TraktListItemWithGuids(
        string DisplayTitle,
        string Title,
        int Year,
        int Season,
        int Episode,
        TraktListItemKind Kind,
        List<string> Guids);
}
