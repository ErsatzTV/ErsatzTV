namespace ErsatzTV.Application.Channels;

public record CreateChannelResult(int ChannelId) : EntityIdResult(ChannelId);
