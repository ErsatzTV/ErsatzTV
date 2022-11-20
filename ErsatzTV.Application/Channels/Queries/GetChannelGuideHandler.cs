using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Iptv;
using Microsoft.IO;

namespace ErsatzTV.Application.Channels;

public class GetChannelGuideHandler : IRequestHandler<GetChannelGuide, ChannelGuide>
{
    private readonly IChannelRepository _channelRepository;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public GetChannelGuideHandler(
        IChannelRepository channelRepository,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager)
    {
        _channelRepository = channelRepository;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
    }

    public Task<ChannelGuide> Handle(GetChannelGuide request, CancellationToken cancellationToken) =>
        _channelRepository.GetAllForGuide()
            .Map(
                channels => new ChannelGuide(
                    _recyclableMemoryStreamManager,
                    request.Scheme,
                    request.Host,
                    request.BaseUrl,
                    channels));
}
