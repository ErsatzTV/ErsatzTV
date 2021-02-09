using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Iptv;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public class GetChannelGuideHandler : IRequestHandler<GetChannelGuide, ChannelGuide>
    {
        private readonly IChannelRepository _channelRepository;

        public GetChannelGuideHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

        public Task<ChannelGuide> Handle(GetChannelGuide request, CancellationToken cancellationToken) =>
            _channelRepository.GetAllForGuide()
                .Map(channels => new ChannelGuide(request.Scheme, request.Host, channels));
    }
}
