using ErsatzTV.Core.Iptv;

namespace ErsatzTV.Application.Channels;

public record GetChannelPlaylist(string Scheme, string Host, string Mode) : IRequest<ChannelPlaylist>;