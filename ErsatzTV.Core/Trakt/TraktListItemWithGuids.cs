using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Trakt;

public record TraktListItemWithGuids(
    int TraktId,
    int Rank,
    string DisplayTitle,
    string Title,
    int? Year,
    int Season,
    int Episode,
    TraktListItemKind Kind,
    List<string> Guids);
