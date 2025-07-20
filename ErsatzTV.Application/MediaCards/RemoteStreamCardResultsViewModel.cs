using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.MediaCards;

public record RemoteStreamCardResultsViewModel(int Count, List<RemoteStreamCardViewModel> Cards, SearchPageMap PageMap);
