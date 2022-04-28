namespace ErsatzTV.Application.MediaCollections;

public record GetPagedSmartCollections(int PageNum, int PageSize) : IRequest<PagedSmartCollectionsViewModel>;
