using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using ErsatzTV.Scanner.Core.Interfaces;

namespace ErsatzTV.Scanner.Application.Plex;

public class SynchronizePlexNetworksHandler : IRequestHandler<SynchronizePlexNetworks, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexTelevisionRepository _plexTelevisionRepository;
    private readonly IScannerProxy _scannerProxy;
    private readonly IPlexNetworkScanner _scanner;

    public SynchronizePlexNetworksHandler(
        IMediaSourceRepository mediaSourceRepository,
        IPlexSecretStore plexSecretStore,
        IPlexNetworkScanner scanner,
        IConfigElementRepository configElementRepository,
        IPlexTelevisionRepository plexTelevisionRepository,
        IScannerProxy scannerProxy)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _plexSecretStore = plexSecretStore;
        _scanner = scanner;
        _configElementRepository = configElementRepository;
        _plexTelevisionRepository = plexTelevisionRepository;
        _scannerProxy = scannerProxy;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        SynchronizePlexNetworks request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request, cancellationToken);
        return await validation.Match(
            p => SynchronizeNetworks(p, cancellationToken),
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizePlexNetworks request,
        CancellationToken cancellationToken)
    {
        Task<Validation<BaseError, ConnectionParameters>> mediaSource = MediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

        return (await mediaSource, await PlexLibraryMustExist(request),
                await ValidateLibraryRefreshInterval(cancellationToken))
            .Apply((connectionParameters, plexLibrary, libraryRefreshInterval) => new RequestParameters(
                connectionParameters,
                plexLibrary,
                request.ForceScan,
                libraryRefreshInterval,
                request.BaseUrl));
    }

    private Task<Validation<BaseError, PlexLibrary>> PlexLibraryMustExist(
        SynchronizePlexNetworks request) =>
        _mediaSourceRepository.GetPlexLibrary(request.PlexLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Plex library {request.PlexLibraryId} does not exist."));

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval(CancellationToken cancellationToken) =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval, cancellationToken)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private Task<Validation<BaseError, PlexMediaSource>> MediaSourceMustExist(
        SynchronizePlexNetworks request) =>
        _mediaSourceRepository.GetPlexByLibraryId(request.PlexLibraryId)
            .Map(o => o.ToValidation<BaseError>(
                $"Plex media source for library {request.PlexLibraryId} does not exist."));

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

    private async Task<Either<BaseError, Unit>> SynchronizeNetworks(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        _scannerProxy.SetBaseUrl(parameters.BaseUrl);

        var lastScan = new DateTimeOffset(
            parameters.Library.LastNetworksScan ?? SystemTime.MinValueUtc,
            TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || parameters.LibraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now)
        {
            Either<BaseError, Unit> result = await _scanner.ScanNetworks(
                parameters.Library,
                parameters.ConnectionParameters.ActiveConnection,
                parameters.ConnectionParameters.PlexServerAuthToken,
                cancellationToken);

            if (result.IsRight)
            {
                parameters.Library.LastNetworksScan = DateTime.UtcNow;
                await _plexTelevisionRepository.UpdateLastNetworksScan(parameters.Library, cancellationToken);
            }

            return result;
        }

        return Unit.Default;
    }

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        PlexLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        string BaseUrl);

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken? PlexServerAuthToken { get; set; }
    }
}
