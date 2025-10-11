namespace ErsatzTV.Application.Playouts;

public record GetBlockPlayoutHistory(int PlayoutId, int BlockId, int PageNum, int PageSize)
    : IRequest<PagedPlayoutHistoryViewModel>;
