using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Hdhr;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Channels;

public class GetChannelLineupHandler : IRequestHandler<GetChannelLineup, List<LineupItem>>
{
    private readonly IChannelRepository _channelRepository;

    public GetChannelLineupHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

    public Task<List<LineupItem>> Handle(GetChannelLineup request, CancellationToken cancellationToken) =>
        _channelRepository.GetAll()
            .Map(channels => channels.Where(c => c.ActiveMode is ChannelActiveMode.Active).Map(c => new LineupItem(request.Scheme, request.Host, c)).ToList());
}
