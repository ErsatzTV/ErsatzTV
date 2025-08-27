using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsHandler(IChannelRepository channelRepository)
    : IRequestHandler<GetAllChannels, List<ChannelViewModel>>
{
    public async Task<List<ChannelViewModel>> Handle(GetAllChannels request, CancellationToken cancellationToken) =>
        await channelRepository.GetAll(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
}
