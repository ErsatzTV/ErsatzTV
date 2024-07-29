namespace ErsatzTV.Application.Playouts;

public record GetPlayoutById(int PlayoutId) : IRequest<Option<PlayoutNameViewModel>>;
