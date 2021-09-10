using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetPagedSmartCollections(int PageNum, int PageSize) : IRequest<PagedSmartCollectionsViewModel>;
}
