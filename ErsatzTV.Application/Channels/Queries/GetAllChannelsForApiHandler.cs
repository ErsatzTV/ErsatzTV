using ErsatzTV.Core.Api.Channels;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsForApiHandler : IRequestHandler<GetAllChannelsForApi, List<ChannelResponseModel>>
{
    private readonly IChannelRepository _channelRepository;

    public GetAllChannelsForApiHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public async Task<List<ChannelResponseModel>> Handle(
        GetAllChannelsForApi request,
        CancellationToken cancellationToken)
    {
        IEnumerable<Channel> channels = Optional(await _channelRepository.GetAll()).Flatten();
        return channels.Map(ProjectToResponseModel).ToList();
    }
}