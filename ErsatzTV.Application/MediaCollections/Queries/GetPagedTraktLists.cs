using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record GetPagedTraktLists(int PageNum, int PageSize) : IRequest<PagedTraktListsViewModel>;