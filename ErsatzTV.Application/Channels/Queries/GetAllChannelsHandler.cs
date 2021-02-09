using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels.Queries
{
    public class GetAllChannelsHandler : IRequestHandler<GetAllChannels, List<ChannelViewModel>>
    {
        private readonly IChannelRepository _channelRepository;

        public GetAllChannelsHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

        public Task<List<ChannelViewModel>> Handle(GetAllChannels request, CancellationToken cancellationToken) =>
            _channelRepository.GetAll().Map(channels => channels.Map(ProjectToViewModel).ToList());
    }
}
