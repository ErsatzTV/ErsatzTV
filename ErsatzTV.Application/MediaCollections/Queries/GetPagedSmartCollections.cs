namespace ErsatzTV.Application.MediaCollections;

public record GetPagedSmartCollections(string Query, int PageNum, int PageSize)
    : IRequest<PagedSmartCollectionsViewModel>;
