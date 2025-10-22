﻿using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Jellyfin;

public class SaveJellyfinSecretsHandler : IRequestHandler<SaveJellyfinSecrets, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IJellyfinBackgroundServiceRequest> _channel;
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMemoryCache _memoryCache;

    public SaveJellyfinSecretsHandler(
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinApiClient jellyfinApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IMemoryCache memoryCache,
        ChannelWriter<IJellyfinBackgroundServiceRequest> channel)
    {
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinApiClient = jellyfinApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _memoryCache = memoryCache;
        _channel = channel;
    }

    public Task<Either<BaseError, Unit>> Handle(SaveJellyfinSecrets request, CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(PerformSave)
            .Bind(v => v.ToEitherAsync());

    private async Task<Validation<BaseError, Parameters>> Validate(SaveJellyfinSecrets request)
    {
        Either<BaseError, JellyfinServerInformation> maybeServerInformation = await _jellyfinApiClient
            .GetServerInformation(request.Secrets.Address, request.Secrets.ApiKey);

        return maybeServerInformation.Match(
            info => Validation<BaseError, Parameters>.Success(new Parameters(request.Secrets, info)),
            error => error);
    }

    private async Task<Unit> PerformSave(Parameters parameters)
    {
        await _jellyfinSecretStore.SaveSecrets(parameters.Secrets);
        await _mediaSourceRepository.UpsertJellyfin(
            parameters.Secrets.Address,
            parameters.ServerInformation.ServerName,
            parameters.ServerInformation.OperatingSystem);
        _memoryCache.Remove(new GetJellyfinConnectionParameters());
        await _channel.WriteAsync(new SynchronizeJellyfinMediaSources());

        return Unit.Default;
    }

    private sealed record Parameters(JellyfinSecrets Secrets, JellyfinServerInformation ServerInformation);
}
