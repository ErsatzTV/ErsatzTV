namespace ErsatzTV.Application.Playouts;

public record PagedPlayoutsViewModel(int TotalCount, List<PlayoutNameViewModel> Page);
