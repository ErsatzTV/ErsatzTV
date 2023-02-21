namespace ErsatzTV.Application.Search;

public record RemoveMediaItems(IReadOnlyCollection<int> MediaItemIds) : IRequest,
    ISearchIndexBackgroundServiceRequest;
