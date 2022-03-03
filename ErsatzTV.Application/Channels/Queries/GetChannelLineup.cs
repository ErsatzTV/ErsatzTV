using System.Collections.Generic;
using ErsatzTV.Core.Hdhr;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record GetChannelLineup(string Scheme, string Host) : IRequest<List<LineupItem>>;