using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record TelevisionEpisodeCardResultsViewModel(
    int Count,
    List<TelevisionEpisodeCardViewModel> Cards,
    Option<SearchPageMap> PageMap);
