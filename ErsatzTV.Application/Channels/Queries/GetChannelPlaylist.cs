using ErsatzTV.Core.Iptv;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public record GetChannelPlaylist(string Scheme, string Host, string Mode) : IRequest<ChannelPlaylist>;
}
