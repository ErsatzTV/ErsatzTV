using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin;

public class SynchronizeJellyfinMediaSourcesHandler : IRequestHandler<SynchronizeJellyfinMediaSources,
    Either<BaseError, List<JellyfinMediaSource>>>
{
    private readonly ChannelWriter<IJellyfinBackgroundServiceRequest> _channel;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeJellyfinMediaSourcesHandler(
        IMediaSourceRepository mediaSourceRepository,
        ChannelWriter<IJellyfinBackgroundServiceRequest> channel)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, List<JellyfinMediaSource>>> Handle(
        SynchronizeJellyfinMediaSources request,
        CancellationToken cancellationToken)
    {
        List<JellyfinMediaSource> mediaSources = await _mediaSourceRepository.GetAllJellyfin();
        foreach (JellyfinMediaSource mediaSource in mediaSources)
        {
            await _channel.WriteAsync(new SynchronizeJellyfinAdminUserId(mediaSource.Id), cancellationToken);
            await _channel.WriteAsync(new SynchronizeJellyfinLibraries(mediaSource.Id), cancellationToken);
        }

        return mediaSources;
    }
}