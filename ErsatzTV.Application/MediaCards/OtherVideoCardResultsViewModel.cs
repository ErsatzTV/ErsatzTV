using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record OtherVideoCardResultsViewModel(
    int Count,
    List<OtherVideoCardViewModel> Cards,
    Option<SearchPageMap> PageMap);