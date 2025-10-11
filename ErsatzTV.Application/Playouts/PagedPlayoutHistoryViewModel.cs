namespace ErsatzTV.Application.Playouts;

public record PagedPlayoutHistoryViewModel(int TotalCount, List<PlayoutHistoryViewModel> Page);
