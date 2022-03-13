namespace ErsatzTV.Application.Channels;

public record GetChannelByNumber(string ChannelNumber) : IRequest<Option<ChannelViewModel>>;
