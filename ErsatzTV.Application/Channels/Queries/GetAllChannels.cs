namespace ErsatzTV.Application.Channels;

public record GetAllChannels(bool ShowDisabled = true) : IRequest<List<ChannelViewModel>>;
