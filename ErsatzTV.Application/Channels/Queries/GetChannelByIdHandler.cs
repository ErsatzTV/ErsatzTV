using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetChannelByIdHandler(IChannelRepository channelRepository)
    : IRequestHandler<GetChannelById, Option<ChannelViewModel>>
{
    public Task<Option<ChannelViewModel>> Handle(GetChannelById request, CancellationToken cancellationToken) =>
        channelRepository.GetChannel(request.Id)
            .MapT(ProjectToViewModel);
}
