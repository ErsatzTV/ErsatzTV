namespace ErsatzTV.Application.Playouts;

public record CheckForOverlappingPlayoutItems(int PlayoutId) : IRequest, IBackgroundServiceRequest;
