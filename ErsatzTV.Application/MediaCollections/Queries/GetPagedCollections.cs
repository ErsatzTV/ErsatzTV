namespace ErsatzTV.Application.MediaCollections;

public record GetPagedCollections(int PageNum, int PageSize) : IRequest<PagedMediaCollectionsViewModel>;