using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Scanner.Application.Plex;

public class SynchronizePlexCollectionsHandler : IRequestHandler<SynchronizePlexCollections, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexCollectionScanner _scanner;

    public SynchronizePlexCollectionsHandler(
        IMediaSourceRepository mediaSourceRepository,
        IPlexSecretStore plexSecretStore,
        IPlexCollectionScanner scanner,
        IConfigElementRepository configElementRepository)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _plexSecretStore = plexSecretStore;
        _scanner = scanner;
        _configElementRepository = configElementRepository;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        SynchronizePlexCollections request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request);
        return await validation.Match(
            p => SynchronizeCollections(p, cancellationToken),
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(SynchronizePlexCollections request)
    {
        Task<Validation<BaseError, ConnectionParameters>> mediaSource = MediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

        return (await mediaSource, await ValidateLibraryRefreshInterval())
            .Apply(
                (connectionParameters, libraryRefreshInterval) => new RequestParameters(
                    connectionParameters,
                    connectionParameters.PlexMediaSource,
                    request.ForceScan,
                    libraryRefreshInterval));
    }

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval() =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private Task<Validation<BaseError, PlexMediaSource>> MediaSourceMustExist(
        SynchronizePlexCollections request) =>
        _mediaSourceRepository.GetPlex(request.PlexMediaSourceId)
            .Map(o => o.ToValidation<BaseError>("Plex media source does not exist."));

    private static Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        PlexMediaSource plexMediaSource)
    {
        Option<PlexConnection> maybeConnection = plexMediaSource.Connections.SingleOrDefault(c => c.IsActive);
        return maybeConnection.Map(connection => new ConnectionParameters(plexMediaSource, connection))
            .ToValidation<BaseError>("Plex media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveToken(
        ConnectionParameters connectionParameters)
    {
        Option<PlexServerAuthToken> maybeToken = await
            _plexSecretStore.GetServerAuthToken(connectionParameters.PlexMediaSource.ClientIdentifier);
        return maybeToken.Map(token => connectionParameters with { PlexServerAuthToken = token })
            .ToValidation<BaseError>("Plex media source requires a token");
    }

    private async Task<Either<BaseError, Unit>> SynchronizeCollections(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        var lastScan = new DateTimeOffset(
            parameters.MediaSource.LastCollectionsScan ?? SystemTime.MinValueUtc,
            TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || parameters.LibraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now)
        {
            Either<BaseError, Unit> result = await _scanner.ScanCollections(
                parameters.ConnectionParameters.ActiveConnection,
                parameters.ConnectionParameters.PlexServerAuthToken,
                cancellationToken);

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
        PlexMediaSource MediaSource,
        bool ForceScan,
        int LibraryRefreshInterval);

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken? PlexServerAuthToken { get; set; }
    }
}
