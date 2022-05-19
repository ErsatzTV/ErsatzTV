using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record MovieCardResultsViewModel(int Count, List<MovieCardViewModel> Cards, Option<SearchPageMap> PageMap);
