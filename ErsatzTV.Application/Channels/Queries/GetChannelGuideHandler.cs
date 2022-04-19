using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Iptv;

namespace ErsatzTV.Application.Channels;

public class GetChannelGuideHandler : IRequestHandler<GetChannelGuide, ChannelGuide>
{
    private readonly IChannelRepository _channelRepository;

    public GetChannelGuideHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public Task<ChannelGuide> Handle(GetChannelGuide request, CancellationToken cancellationToken) =>
        _channelRepository.GetAllForGuide()
            .Map(channels => new ChannelGuide(request.Scheme, request.Host, channels));
}
