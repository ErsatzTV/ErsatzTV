using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsHandler : IRequestHandler<GetAllChannels, List<ChannelViewModel>>
{
    private readonly IChannelRepository _channelRepository;

    public GetAllChannelsHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public async Task<List<ChannelViewModel>> Handle(GetAllChannels request, CancellationToken cancellationToken) =>
        Optional(await _channelRepository.GetAll()).Flatten().Map(ProjectToViewModel).ToList();
}
