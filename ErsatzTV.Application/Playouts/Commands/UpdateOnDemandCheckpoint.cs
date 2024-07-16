namespace ErsatzTV.Application.Playouts;

public record UpdateOnDemandCheckpoint(string ChannelNumber, DateTimeOffset Checkpoint)
    : IRequest;
