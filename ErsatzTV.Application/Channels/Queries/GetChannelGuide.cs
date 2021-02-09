using ErsatzTV.Core.Iptv;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public record GetChannelGuide(string Scheme, string Host) : IRequest<ChannelGuide>;
}
