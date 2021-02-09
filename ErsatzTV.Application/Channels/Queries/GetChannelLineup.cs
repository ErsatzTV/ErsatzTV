using System.Collections.Generic;
using ErsatzTV.Core.Hdhr;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public record GetChannelLineup(string Scheme, string Host) : IRequest<List<LineupItem>>;
}
