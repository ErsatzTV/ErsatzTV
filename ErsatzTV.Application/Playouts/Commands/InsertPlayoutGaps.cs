namespace ErsatzTV.Application.Playouts;

public record InsertPlayoutGaps(int PlayoutId) : IRequest, IBackgroundServiceRequest;
