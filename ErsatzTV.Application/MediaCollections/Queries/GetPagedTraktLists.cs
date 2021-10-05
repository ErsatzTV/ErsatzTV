using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetPagedTraktLists(int PageNum, int PageSize) : IRequest<PagedTraktListsViewModel>;
}
