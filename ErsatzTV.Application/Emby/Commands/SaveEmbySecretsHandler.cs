using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Emby;

public class SaveEmbySecretsHandler : IRequestHandler<SaveEmbySecrets, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IEmbyBackgroundServiceRequest> _channel;
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IEmbySecretStore _embySecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SaveEmbySecretsHandler(
        IEmbySecretStore embySecretStore,
        IEmbyApiClient embyApiClient,
        IMediaSourceRepository mediaSourceRepository,
        ChannelWriter<IEmbyBackgroundServiceRequest> channel)
    {
        _embySecretStore = embySecretStore;
        _embyApiClient = embyApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _channel = channel;
    }

    public Task<Either<BaseError, Unit>> Handle(SaveEmbySecrets request, CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(PerformSave)
            .Bind(v => v.ToEitherAsync());

    private async Task<Validation<BaseError, Parameters>> Validate(SaveEmbySecrets request)
    {
        Either<BaseError, EmbyServerInformation> maybeServerInformation = await _embyApiClient
            .GetServerInformation(request.Secrets.Address, request.Secrets.ApiKey);

        return maybeServerInformation.Match(
            info => Validation<BaseError, Parameters>.Success(new Parameters(request.Secrets, info)),
            error => error);
    }

    private async Task<Unit> PerformSave(Parameters parameters)
    {
        await _embySecretStore.SaveSecrets(parameters.Secrets);
        await _mediaSourceRepository.UpsertEmby(
            parameters.Secrets.Address,
            parameters.ServerInformation.ServerName,
            parameters.ServerInformation.OperatingSystem);
        await _channel.WriteAsync(new SynchronizeEmbyMediaSources());

        return Unit.Default;
    }

    private record Parameters(EmbySecrets Secrets, EmbyServerInformation ServerInformation);
}
