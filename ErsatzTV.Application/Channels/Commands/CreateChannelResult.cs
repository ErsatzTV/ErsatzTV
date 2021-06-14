namespace ErsatzTV.Application.Channels.Commands
{
    public record CreateChannelResult(int ChannelId) : EntityIdResult(ChannelId);
}
