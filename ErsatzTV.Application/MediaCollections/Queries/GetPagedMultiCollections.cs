using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetPagedMultiCollections(int PageNum, int PageSize) : IRequest<PagedMultiCollectionsViewModel>;
}
