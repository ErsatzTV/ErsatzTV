namespace ErsatzTV.Application.Playouts;

public record GetPlayoutAlternateSchedules(int PlayoutId) : IRequest<List<PlayoutAlternateScheduleViewModel>>;
