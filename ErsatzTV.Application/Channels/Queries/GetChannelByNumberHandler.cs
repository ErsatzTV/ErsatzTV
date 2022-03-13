using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetChannelByNumberHandler : IRequestHandler<GetChannelByNumber, Option<ChannelViewModel>>
{
    private readonly IChannelRepository _channelRepository;

    public GetChannelByNumberHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public Task<Option<ChannelViewModel>> Handle(GetChannelByNumber request, CancellationToken cancellationToken) =>
        _channelRepository.GetByNumber(request.ChannelNumber).MapT(ProjectToViewModel);
}