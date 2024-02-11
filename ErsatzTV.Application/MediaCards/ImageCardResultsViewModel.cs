using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record ImageCardResultsViewModel(int Count, List<ImageCardViewModel> Cards, SearchPageMap PageMap);
