using ErsatzTV.Core.Iptv;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record GetChannelPlaylist(string Scheme, string Host, string Mode) : IRequest<ChannelPlaylist>;