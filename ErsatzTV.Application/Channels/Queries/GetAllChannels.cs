using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public record GetAllChannels : IRequest<List<ChannelViewModel>>;
}
