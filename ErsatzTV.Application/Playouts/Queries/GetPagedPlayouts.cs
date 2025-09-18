namespace ErsatzTV.Application.Playouts;

public record GetPagedPlayouts(string Query, int PageNum, int PageSize) : IRequest<PagedPlayoutsViewModel>;
