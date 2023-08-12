using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record ArtistCardResultsViewModel(
    int Count,
    List<ArtistCardViewModel> Cards,
    SearchPageMap PageMap);
