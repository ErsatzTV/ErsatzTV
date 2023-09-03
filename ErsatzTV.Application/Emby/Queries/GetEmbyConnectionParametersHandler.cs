using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Emby;

public class GetEmbyConnectionParametersHandler : IRequestHandler<GetEmbyConnectionParameters,
    Either<BaseError, EmbyConnectionParametersViewModel>>
{
    private readonly IEmbySecretStore _embySecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMemoryCache _memoryCache;

    public GetEmbyConnectionParametersHandler(
        IMemoryCache memoryCache,
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore)
    {
        _memoryCache = memoryCache;
        _mediaSourceRepository = mediaSourceRepository;
        _embySecretStore = embySecretStore;
    }

    public async Task<Either<BaseError, EmbyConnectionParametersViewModel>> Handle(
        GetEmbyConnectionParameters request,
        CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(request, out EmbyConnectionParametersViewModel parameters))
        {
            return parameters;
        }

        Either<BaseError, EmbyConnectionParametersViewModel> maybeParameters =
            await Validate()
                .MapT(cp => new EmbyConnectionParametersViewModel(cp.ActiveConnection.Address, cp.ApiKey))
                .Map(v => v.ToEither<EmbyConnectionParametersViewModel>());

        return maybeParameters.Match(
            p =>
            {
                _memoryCache.Set(request, p, TimeSpan.FromHours(1));
                return maybeParameters;
            },
            error => error);
    }

    private Task<Validation<BaseError, ConnectionParameters>> Validate() =>
        EmbyMediaSourceMustExist()
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist() =>
        _mediaSourceRepository.GetAllEmby().Map(list => list.HeadOrNone())
            .Map(
                v => v.ToValidation<BaseError>(
                    "Emby media source does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        EmbyMediaSource embyMediaSource)
    {
        Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.FirstOrDefault();
        return maybeConnection.Map(connection => new ConnectionParameters(embyMediaSource, connection))
            .ToValidation<BaseError>("Emby media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveApiKey(
        ConnectionParameters connectionParameters)
    {
        EmbySecrets secrets = await _embySecretStore.ReadSecrets();
        return Optional(secrets.Address == connectionParameters.ActiveConnection.Address)
            .Where(match => match)
            .Map(_ => connectionParameters with { ApiKey = secrets.ApiKey })
            .ToValidation<BaseError>("Emby media source requires an api key");
    }

    private sealed record ConnectionParameters(
        EmbyMediaSource EmbyMediaSource,
        EmbyConnection ActiveConnection)
    {
        public string ApiKey { get; set; }
    }
}
