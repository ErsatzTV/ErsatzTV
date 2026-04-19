namespace ErsatzTV.Application.Playouts;

public record SyncNextPlayout(string ChannelNumber) : IRequest, IBackgroundServiceRequest;
