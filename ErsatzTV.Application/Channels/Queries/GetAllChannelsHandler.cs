using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using static ErsatzTV.Application.Channels.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsHandler : IRequestHandler<GetAllChannels, List<ChannelViewModel>>
{
    private readonly IChannelRepository _channelRepository;

    public GetAllChannelsHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public async Task<List<ChannelViewModel>> Handle(GetAllChannels request, CancellationToken cancellationToken) =>
        Optional(await _channelRepository.GetAll()).Flatten().Map(ProjectToViewModel).ToList();
}