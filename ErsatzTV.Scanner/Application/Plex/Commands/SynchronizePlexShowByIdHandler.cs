using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Plex;

public class SynchronizePlexShowByIdHandler : IRequestHandler<SynchronizePlexShowById, Either<BaseError, string>>
{
    private readonly ILogger<SynchronizePlexShowByIdHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexTelevisionLibraryScanner _plexTelevisionLibraryScanner;
    private readonly IScannerProxy _scannerProxy;
    private readonly IPlexTelevisionRepository _plexTelevisionRepository;

    public SynchronizePlexShowByIdHandler(
        IScannerProxy scannerProxy,
        IPlexTelevisionRepository plexTelevisionRepository,
        IMediaSourceRepository mediaSourceRepository,
        IPlexSecretStore plexSecretStore,
        IPlexTelevisionLibraryScanner plexTelevisionLibraryScanner,
        ILogger<SynchronizePlexShowByIdHandler> logger)
    {
        _scannerProxy = scannerProxy;
        _plexTelevisionRepository = plexTelevisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexSecretStore = plexSecretStore;
        _plexTelevisionLibraryScanner = plexTelevisionLibraryScanner;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizePlexShowById request,
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
        if (parameters.Library.MediaKind != LibraryMediaKind.Shows)
        {
            return BaseError.New($"Library {parameters.Library.Name} is not a TV show library");
        }

        _scannerProxy.SetBaseUrl(parameters.BaseUrl);

        _logger.LogInformation(
            "Starting targeted scan for show '{ShowTitle}' in Plex library {LibraryName}",
            parameters.ShowTitle,
            parameters.Library.Name);

        Either<BaseError, Unit> result = await _plexTelevisionLibraryScanner.ScanSingleShow(
            parameters.ConnectionParameters.ActiveConnection,
            parameters.ConnectionParameters.PlexServerAuthToken,
            parameters.Library,
            parameters.ShowKey,
            parameters.ShowTitle,
            parameters.DeepScan,
            cancellationToken);

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError("Error synchronizing Plex show '{ShowTitle}': {Error}", parameters.ShowTitle, error);
        }

        return result.Map(_ => $"Show '{parameters.ShowTitle}' in {parameters.Library.Name}");
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizePlexShowById request,
        CancellationToken cancellationToken) =>
        (await ValidateConnection(request), await PlexLibraryMustExist(request),
            await PlexShowMustExist(request, cancellationToken))
        .Apply((connectionParameters, plexLibrary, titleKey) =>
            new RequestParameters(
                connectionParameters,
                plexLibrary,
                titleKey.Key,
                titleKey.Title,
                request.DeepScan,
                request.BaseUrl
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizePlexShowById request) =>
        PlexMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

    private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
        SynchronizePlexShowById request) =>
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
        SynchronizePlexShowById request) =>
        _mediaSourceRepository.GetPlexLibrary(request.PlexLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Plex library {request.PlexLibraryId} does not exist."));

    private Task<Validation<BaseError, PlexShowTitleKeyResult>> PlexShowMustExist(
        SynchronizePlexShowById request,
        CancellationToken cancellationToken) =>
        _plexTelevisionRepository.GetShowTitleKey(request.PlexLibraryId, request.ShowId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>(
                $"Plex show {request.ShowId} does not exist in library {request.PlexLibraryId}."));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        PlexLibrary Library,
        string ShowKey,
        string ShowTitle,
        bool DeepScan,
        string BaseUrl);

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken? PlexServerAuthToken { get; set; }
    }
}
