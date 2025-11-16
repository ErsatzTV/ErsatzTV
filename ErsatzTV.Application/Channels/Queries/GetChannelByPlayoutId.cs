namespace ErsatzTV.Application.Channels;

public record GetChannelByPlayoutId(int PlayoutId) : IRequest<Option<ChannelViewModel>>;
