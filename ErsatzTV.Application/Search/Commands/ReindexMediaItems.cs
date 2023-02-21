namespace ErsatzTV.Application.Search;

public record ReindexMediaItems(IReadOnlyCollection<int> MediaItemIds) : IRequest,
    ISearchIndexBackgroundServiceRequest;
