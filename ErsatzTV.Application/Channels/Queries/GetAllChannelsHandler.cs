using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsHandler(IChannelRepository channelRepository)
    : IRequestHandler<GetAllChannels, List<ChannelViewModel>>
{
    public async Task<List<ChannelViewModel>> Handle(GetAllChannels request, CancellationToken cancellationToken) =>
        await channelRepository.GetAll(cancellationToken)
            .Map(list => list.Map(c => ProjectToViewModel(c, GetPlayoutsCount(c))).ToList());

    private static int GetPlayoutsCount(Channel channel)
    {
        var result = 0;

        if (channel.Playouts != null)
        {
            result += channel.Playouts.Count;
        }

        if (channel.PlayoutSource is ChannelPlayoutSource.Mirror && channel.MirrorSourceChannel?.Playouts != null)
        {
            result += channel.MirrorSourceChannel.Playouts.Count;
        }

        return result;
    }
}
