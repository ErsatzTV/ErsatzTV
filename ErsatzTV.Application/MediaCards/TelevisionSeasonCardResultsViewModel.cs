using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record TelevisionSeasonCardResultsViewModel(
    int Count,
    List<TelevisionSeasonCardViewModel> Cards,
    Option<SearchPageMap> PageMap);
