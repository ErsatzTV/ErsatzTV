using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Hdhr;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public class GetChannelLineupHandler : IRequestHandler<GetChannelLineup, List<LineupItem>>
    {
        private readonly IChannelRepository _channelRepository;

        public GetChannelLineupHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

        public Task<List<LineupItem>> Handle(GetChannelLineup request, CancellationToken cancellationToken) =>
            _channelRepository.GetAll()
                .Map(channels => channels.Map(c => new LineupItem(request.Scheme, request.Host, c)).ToList());
    }
}
