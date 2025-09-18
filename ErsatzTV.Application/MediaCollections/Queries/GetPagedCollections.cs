namespace ErsatzTV.Application.MediaCollections;

public record GetPagedCollections(string Query, int PageNum, int PageSize) : IRequest<PagedMediaCollectionsViewModel>;
