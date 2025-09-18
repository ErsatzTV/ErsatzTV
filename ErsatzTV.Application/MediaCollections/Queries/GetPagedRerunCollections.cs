namespace ErsatzTV.Application.MediaCollections;

public record GetPagedRerunCollections(string Query, int PageNum, int PageSize)
    : IRequest<PagedRerunCollectionsViewModel>;
