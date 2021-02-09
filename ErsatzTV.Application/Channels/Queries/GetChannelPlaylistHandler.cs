using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Iptv;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public class GetChannelPlaylistHandler : IRequestHandler<GetChannelPlaylist, ChannelPlaylist>
    {
        private readonly IChannelRepository _channelRepository;

        public GetChannelPlaylistHandler(IChannelRepository channelRepository) =>
            _channelRepository = channelRepository;

        public Task<ChannelPlaylist> Handle(GetChannelPlaylist request, CancellationToken cancellationToken) =>
            _channelRepository.GetAll()
                .Map(channels => new ChannelPlaylist(request.Scheme, request.Host, channels));
    }
}
