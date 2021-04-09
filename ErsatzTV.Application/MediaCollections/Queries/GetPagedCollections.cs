using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetPagedCollections(int PageNum, int PageSize) : IRequest<PagedMediaCollectionsViewModel>;
}
