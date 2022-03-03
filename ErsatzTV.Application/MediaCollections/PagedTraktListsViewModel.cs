namespace ErsatzTV.Application.MediaCollections;

public record PagedTraktListsViewModel(int TotalCount, List<TraktListViewModel> Page);