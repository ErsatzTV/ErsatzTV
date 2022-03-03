using ErsatzTV.Core.Iptv;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record GetChannelGuide(string Scheme, string Host) : IRequest<ChannelGuide>;