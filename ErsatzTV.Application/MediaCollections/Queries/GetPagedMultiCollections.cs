namespace ErsatzTV.Application.MediaCollections;

public record GetPagedMultiCollections(string Query, int PageNum, int PageSize)
    : IRequest<PagedMultiCollectionsViewModel>;
