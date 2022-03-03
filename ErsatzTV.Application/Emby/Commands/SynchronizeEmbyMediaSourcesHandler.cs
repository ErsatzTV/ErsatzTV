using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Emby;

public class SynchronizeEmbyMediaSourcesHandler : IRequestHandler<SynchronizeEmbyMediaSources,
    Either<BaseError, List<EmbyMediaSource>>>
{
    private readonly ChannelWriter<IEmbyBackgroundServiceRequest> _channel;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyMediaSourcesHandler(
        IMediaSourceRepository mediaSourceRepository,
        ChannelWriter<IEmbyBackgroundServiceRequest> channel)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, List<EmbyMediaSource>>> Handle(
        SynchronizeEmbyMediaSources request,
        CancellationToken cancellationToken)
    {
        List<EmbyMediaSource> mediaSources = await _mediaSourceRepository.GetAllEmby();
        foreach (EmbyMediaSource mediaSource in mediaSources)
        {
            // await _channel.WriteAsync(new SynchronizeEmbyAdminUserId(mediaSource.Id), cancellationToken);
            await _channel.WriteAsync(new SynchronizeEmbyLibraries(mediaSource.Id), cancellationToken);
        }

        return mediaSources;
    }
}