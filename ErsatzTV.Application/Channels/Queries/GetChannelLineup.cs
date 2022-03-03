using ErsatzTV.Core.Hdhr;

namespace ErsatzTV.Application.Channels;

public record GetChannelLineup(string Scheme, string Host) : IRequest<List<LineupItem>>;