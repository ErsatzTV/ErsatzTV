namespace ErsatzTV.Application.Channels;

public record GetChannelResolution(string ChannelNumber) : IRequest<Option<ResolutionViewModel>>;
