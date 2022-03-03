using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetChannelByIdHandler : IRequestHandler<GetChannelById, Option<ChannelViewModel>>
{
    private readonly IChannelRepository _channelRepository;

    public GetChannelByIdHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public Task<Option<ChannelViewModel>> Handle(GetChannelById request, CancellationToken cancellationToken) =>
        _channelRepository.Get(request.Id)
            .MapT(ProjectToViewModel);
}