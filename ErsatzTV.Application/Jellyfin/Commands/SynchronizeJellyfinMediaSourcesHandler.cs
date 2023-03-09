using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Jellyfin;

public class SynchronizeJellyfinMediaSourcesHandler : IRequestHandler<SynchronizeJellyfinMediaSources,
    Either<BaseError, List<JellyfinMediaSource>>>
{
    private readonly ChannelWriter<IScannerBackgroundServiceRequest> _scannerWorkerChannel;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeJellyfinMediaSourcesHandler(
        IMediaSourceRepository mediaSourceRepository,
        ChannelWriter<IScannerBackgroundServiceRequest> scannerWorkerChannel)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _scannerWorkerChannel = scannerWorkerChannel;
    }

    public async Task<Either<BaseError, List<JellyfinMediaSource>>> Handle(
        SynchronizeJellyfinMediaSources request,
        CancellationToken cancellationToken)
    {
        List<JellyfinMediaSource> mediaSources = await _mediaSourceRepository.GetAllJellyfin();
        foreach (JellyfinMediaSource mediaSource in mediaSources)
        {
            await _scannerWorkerChannel.WriteAsync(new SynchronizeJellyfinAdminUserId(mediaSource.Id), cancellationToken);
            await _scannerWorkerChannel.WriteAsync(new SynchronizeJellyfinLibraries(mediaSource.Id), cancellationToken);
        }

        return mediaSources;
    }
}
