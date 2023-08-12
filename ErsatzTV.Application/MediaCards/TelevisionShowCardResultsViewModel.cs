using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record TelevisionShowCardResultsViewModel(
    int Count,
    List<TelevisionShowCardViewModel> Cards,
    SearchPageMap PageMap);
