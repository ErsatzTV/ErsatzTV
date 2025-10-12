using ErsatzTV.Core.Api.Channels;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsForApiHandler(IChannelRepository channelRepository)
    : IRequestHandler<GetAllChannelsForApi, List<ChannelResponseModel>>
{
    public async Task<List<ChannelResponseModel>> Handle(
        GetAllChannelsForApi request,
        CancellationToken cancellationToken)
    {
        IEnumerable<Channel> channels = Optional(await channelRepository.GetAll(cancellationToken)).Flatten();
        return channels.Map(ProjectToResponseModel).ToList();
    }
}
