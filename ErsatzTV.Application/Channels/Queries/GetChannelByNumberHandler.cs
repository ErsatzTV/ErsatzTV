using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetChannelByNumberHandler(IChannelRepository channelRepository)
    : IRequestHandler<GetChannelByNumber, Option<ChannelViewModel>>
{
    public Task<Option<ChannelViewModel>> Handle(GetChannelByNumber request, CancellationToken cancellationToken) =>
        channelRepository.GetByNumber(request.ChannelNumber).MapT(c => ProjectToViewModel(c, 0));
}
