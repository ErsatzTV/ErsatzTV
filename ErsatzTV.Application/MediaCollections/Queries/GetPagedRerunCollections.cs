namespace ErsatzTV.Application.MediaCollections;

public record GetPagedRerunCollections(int PageNum, int PageSize) : IRequest<PagedRerunCollectionsViewModel>;
