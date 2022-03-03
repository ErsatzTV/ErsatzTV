using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record SongCardResultsViewModel(
    int Count,
    List<SongCardViewModel> Cards,
    Option<SearchPageMap> PageMap);