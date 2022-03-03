using MediatR;

namespace ErsatzTV.Application.Playouts;

public record GetFuturePlayoutItemsById(int PlayoutId, bool ShowFiller, int PageNum, int PageSize) : IRequest<PagedPlayoutItemsViewModel>;