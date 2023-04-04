namespace ErsatzTV.Application.Channels;

public record RefreshChannelData(string ChannelNumber) : IRequest, IBackgroundServiceRequest;
