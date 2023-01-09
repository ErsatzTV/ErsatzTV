namespace ErsatzTV.Application.Channels;

public record GetChannelNameByPlayoutId(int PlayoutId) : IRequest<Option<string>>;
