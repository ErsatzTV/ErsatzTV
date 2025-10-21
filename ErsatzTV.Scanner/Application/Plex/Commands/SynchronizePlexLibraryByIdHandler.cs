using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Plex;

public class SynchronizePlexLibraryByIdHandler : IRequestHandler<SynchronizePlexLibraryById, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizePlexLibraryByIdHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IScannerProxy _scannerProxy;
    private readonly IPlexMovieLibraryScanner _plexMovieLibraryScanner;
    private readonly IPlexOtherVideoLibraryScanner _plexOtherVideoLibraryScanner;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexTelevisionLibraryScanner _plexTelevisionLibraryScanner;

    public SynchronizePlexLibraryByIdHandler(
        IScannerProxy scannerProxy,
        IMediaSourceRepository mediaSourceRepository,
        IConfigElementRepository configElementRepository,
        IPlexSecretStore plexSecretStore,
        IPlexMovieLibraryScanner plexMovieLibraryScanner,
        IPlexOtherVideoLibraryScanner plexOtherVideoLibraryScanner,
        IPlexTelevisionLibraryScanner plexTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        ILogger<SynchronizePlexLibraryByIdHandler> logger)
    {
        _scannerProxy = scannerProxy;
        _mediaSourceRepository = mediaSourceRepository;
        _configElementRepository = configElementRepository;
        _plexSecretStore = plexSecretStore;
        _plexMovieLibraryScanner = plexMovieLibraryScanner;
        _plexOtherVideoLibraryScanner = plexOtherVideoLibraryScanner;
        _plexTelevisionLibraryScanner = plexTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizePlexLibraryById request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request, cancellationToken);
        return await validation.Match(
            parameters => Synchronize(parameters, cancellationToken),
            error => Task.FromResult<Either<BaseError, string>>(error.Join()));
    }

    private async Task<Either<BaseError, string>> Synchronize(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        _scannerProxy.SetBaseUrl(parameters.BaseUrl);

        var lastScan = new DateTimeOffset(parameters.Library.LastScan ?? SystemTime.MinValueUtc, TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || parameters.LibraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now)
        {
            Either<BaseError, Unit> result = parameters.Library.MediaKind switch
            {
                LibraryMediaKind.Movies =>
                    await _plexMovieLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection,
                        parameters.ConnectionParameters.PlexServerAuthToken,
                        parameters.Library,
                        parameters.DeepScan,
                        cancellationToken),
                LibraryMediaKind.OtherVideos =>
                    await _plexOtherVideoLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection,
                        parameters.ConnectionParameters.PlexServerAuthToken,
                        parameters.Library,
                        parameters.DeepScan,
                        cancellationToken),
                LibraryMediaKind.Shows =>
                    await _plexTelevisionLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection,
                        parameters.ConnectionParameters.PlexServerAuthToken,
                        parameters.Library,
                        parameters.DeepScan,
                        cancellationToken),
                _ => Unit.Default
            };

            if (result.IsRight)
            {
                parameters.Library.LastScan = DateTime.UtcNow;
                await _libraryRepository.UpdateLastScan(parameters.Library);
            }

            foreach (BaseError error in result.LeftToSeq())
            {
                _logger.LogError("Error synchronizing plex library: {Error}", error);
            }

            return result.Map(_ => parameters.Library.Name);
        }

        _logger.LogDebug(
            "Skipping unforced scan of plex media library {Name}",
            parameters.Library.Name);

        return parameters.Library.Name;
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizePlexLibraryById request,
        CancellationToken cancellationToken) =>
        (await ValidateConnection(request), await PlexLibraryMustExist(request),
            await ValidateLibraryRefreshInterval(cancellationToken))
        .Apply((connectionParameters, plexLibrary, libraryRefreshInterval) =>
            new RequestParameters(
                connectionParameters,
                plexLibrary,
                request.ForceScan,
                libraryRefreshInterval,
                request.DeepScan,
                request.BaseUrl
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizePlexLibraryById request) =>
        PlexMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

    private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
        SynchronizePlexLibraryById request) =>
        _mediaSourceRepository.GetPlexByLibraryId(request.PlexLibraryId)
            .Map(v => v.ToValidation<BaseError>(
                $"Plex media source for library {request.PlexLibraryId} does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        PlexMediaSource plexMediaSource)
    {
        Option<PlexConnection> maybeConnection =
            plexMediaSource.Connections.SingleOrDefault(c => c.IsActive);
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

    private Task<Validation<BaseError, PlexLibrary>> PlexLibraryMustExist(
        SynchronizePlexLibraryById request) =>
        _mediaSourceRepository.GetPlexLibrary(request.PlexLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Plex library {request.PlexLibraryId} does not exist."));

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval(CancellationToken cancellationToken) =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval, cancellationToken)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        PlexLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        bool DeepScan,
        string BaseUrl);

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken? PlexServerAuthToken { get; set; }
    }
}
