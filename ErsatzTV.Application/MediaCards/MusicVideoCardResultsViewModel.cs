using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record MusicVideoCardResultsViewModel(
    int Count,
    List<MusicVideoCardViewModel> Cards,
    Option<SearchPageMap> PageMap);
