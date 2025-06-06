﻿using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Plex;

namespace ErsatzTV.Application.Plex;

public class TryCompletePlexPinFlowHandler : IRequestHandler<TryCompletePlexPinFlow, Either<BaseError, bool>>
{
    private readonly ChannelWriter<IPlexBackgroundServiceRequest> _channel;
    private readonly IPlexTvApiClient _plexTvApiClient;

    public TryCompletePlexPinFlowHandler(
        IPlexTvApiClient plexTvApiClient,
        ChannelWriter<IPlexBackgroundServiceRequest> channel)
    {
        _plexTvApiClient = plexTvApiClient;
        _channel = channel;
    }

    public async Task<Either<BaseError, bool>>
        Handle(TryCompletePlexPinFlow request, CancellationToken cancellationToken)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
        CancellationToken token = linkedTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            bool result = await _plexTvApiClient.TryCompletePinFlow(request.AuthPin);
            if (result)
            {
                await _channel.WriteAsync(new SynchronizePlexMediaSources(), token);
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }

        return false;
    }
}
