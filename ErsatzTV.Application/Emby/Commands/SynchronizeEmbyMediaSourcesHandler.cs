using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Emby;

public class SynchronizeEmbyMediaSourcesHandler : IRequestHandler<SynchronizeEmbyMediaSources,
    Either<BaseError, List<EmbyMediaSource>>>
{
    private readonly ChannelWriter<IScannerBackgroundServiceRequest> _scannerWorkerChannel;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyMediaSourcesHandler(
        IMediaSourceRepository mediaSourceRepository,
        ChannelWriter<IScannerBackgroundServiceRequest> scannerWorkerChannel)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _scannerWorkerChannel = scannerWorkerChannel;
    }

    public async Task<Either<BaseError, List<EmbyMediaSource>>> Handle(
        SynchronizeEmbyMediaSources request,
        CancellationToken cancellationToken)
    {
        List<EmbyMediaSource> mediaSources = await _mediaSourceRepository.GetAllEmby();
        foreach (EmbyMediaSource mediaSource in mediaSources)
        {
            // await _channel.WriteAsync(new SynchronizeEmbyAdminUserId(mediaSource.Id), cancellationToken);
            await _scannerWorkerChannel.WriteAsync(new SynchronizeEmbyLibraries(mediaSource.Id), cancellationToken);
        }

        return mediaSources;
    }
}
