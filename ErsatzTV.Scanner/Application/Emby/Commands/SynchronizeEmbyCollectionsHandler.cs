using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces;

namespace ErsatzTV.Scanner.Application.Emby;

public class SynchronizeEmbyCollectionsHandler : IRequestHandler<SynchronizeEmbyCollections, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IScannerProxy _scannerProxy;
    private readonly IEmbySecretStore _embySecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IEmbyCollectionScanner _scanner;

    public SynchronizeEmbyCollectionsHandler(
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore,
        IEmbyCollectionScanner scanner,
        IConfigElementRepository configElementRepository,
        IScannerProxy scannerProxy)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _embySecretStore = embySecretStore;
        _scanner = scanner;
        _configElementRepository = configElementRepository;
        _scannerProxy = scannerProxy;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request, cancellationToken);
        return await validation.Match(
            SynchronizeCollections,
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken)
    {
        Task<Validation<BaseError, ConnectionParameters>> mediaSource = MediaSourceMustExist(request, cancellationToken)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

        return (await mediaSource, await ValidateLibraryRefreshInterval(cancellationToken))
            .Apply((connectionParameters, libraryRefreshInterval) => new RequestParameters(
                connectionParameters,
                connectionParameters.MediaSource,
                request.ForceScan,
                request.DeepScan,
                libraryRefreshInterval,
                request.BaseUrl));
    }

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval(CancellationToken cancellationToken) =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval, cancellationToken)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private Task<Validation<BaseError, EmbyMediaSource>> MediaSourceMustExist(
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetEmby(request.EmbyMediaSourceId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Emby media source does not exist."));

    private static Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        EmbyMediaSource embyMediaSource)
    {
        Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(connection, embyMediaSource))
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

    private async Task<Either<BaseError, Unit>> SynchronizeCollections(RequestParameters parameters)
    {
        _scannerProxy.SetBaseUrl(parameters.BaseUrl);

        var lastScan = new DateTimeOffset(
            parameters.MediaSource.LastCollectionsScan ?? SystemTime.MinValueUtc,
            TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || parameters.LibraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now)
        {
            Either<BaseError, Unit> result = await _scanner.ScanCollections(
                parameters.ConnectionParameters.ActiveConnection.Address,
                parameters.ConnectionParameters.ApiKey,
                parameters.DeepScan);

            if (result.IsRight)
            {
                parameters.MediaSource.LastCollectionsScan = DateTime.UtcNow;
                await _mediaSourceRepository.UpdateLastCollectionScan(parameters.MediaSource);
            }

            return result;
        }

        return Unit.Default;
    }

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        EmbyMediaSource MediaSource,
        bool ForceScan,
        bool DeepScan,
        int LibraryRefreshInterval,
        string BaseUrl);

    private record ConnectionParameters(EmbyConnection ActiveConnection, EmbyMediaSource MediaSource)
    {
        public string? ApiKey { get; init; }
    }
}
