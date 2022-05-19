namespace ErsatzTV.Application.MediaCollections;

public record GetPagedMultiCollections(int PageNum, int PageSize) : IRequest<PagedMultiCollectionsViewModel>;
