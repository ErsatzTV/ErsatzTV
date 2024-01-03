using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public class
    SynchronizeJellyfinCollectionsHandler : IRequestHandler<SynchronizeJellyfinCollections, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IJellyfinCollectionScanner _scanner;

    public SynchronizeJellyfinCollectionsHandler(
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinCollectionScanner scanner,
        IConfigElementRepository configElementRepository)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _scanner = scanner;
        _configElementRepository = configElementRepository;
    }


    public async Task<Either<BaseError, Unit>> Handle(
        SynchronizeJellyfinCollections request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request);
        return await validation.Match(
            SynchronizeCollections,
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(SynchronizeJellyfinCollections request)
    {
        Task<Validation<BaseError, ConnectionParameters>> mediaSource = MediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

        return (await mediaSource, await ValidateLibraryRefreshInterval())
            .Apply(
                (connectionParameters, libraryRefreshInterval) => new RequestParameters(
                    connectionParameters,
                    connectionParameters.MediaSource,
                    request.ForceScan,
                    libraryRefreshInterval));
    }

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval() =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private Task<Validation<BaseError, JellyfinMediaSource>> MediaSourceMustExist(
        SynchronizeJellyfinCollections request) =>
        _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId)
            .Map(o => o.ToValidation<BaseError>("Jellyfin media source does not exist."));

    private static Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        JellyfinMediaSource jellyfinMediaSource)
    {
        Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(jellyfinMediaSource, connection))
            .ToValidation<BaseError>("Jellyfin media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveApiKey(
        ConnectionParameters connectionParameters)
    {
        JellyfinSecrets secrets = await _jellyfinSecretStore.ReadSecrets();
        return Optional(secrets.Address == connectionParameters.ActiveConnection.Address)
            .Where(match => match)
            .Map(_ => connectionParameters with { ApiKey = secrets.ApiKey })
            .ToValidation<BaseError>("Jellyfin media source requires an api key");
    }

    private async Task<Either<BaseError, Unit>> SynchronizeCollections(RequestParameters parameters)
    {
        var lastScan = new DateTimeOffset(
            parameters.MediaSource.LastCollectionsScan ?? SystemTime.MinValueUtc,
            TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || parameters.LibraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now)
        {
            Either<BaseError, Unit> result = await _scanner.ScanCollections(
                parameters.ConnectionParameters.ActiveConnection.Address,
                parameters.ConnectionParameters.ApiKey,
                parameters.MediaSource.Id);

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
        JellyfinMediaSource MediaSource,
        bool ForceScan,
        int LibraryRefreshInterval);

    private record ConnectionParameters(JellyfinMediaSource MediaSource, JellyfinConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}
