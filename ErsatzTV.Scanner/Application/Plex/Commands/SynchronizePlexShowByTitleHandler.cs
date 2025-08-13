using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Plex;

public class SynchronizePlexShowByTitleHandler : IRequestHandler<SynchronizePlexShowByTitle, Either<BaseError, string>>
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizePlexShowByTitleHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMediator _mediator;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexTelevisionLibraryScanner _plexTelevisionLibraryScanner;

    public SynchronizePlexShowByTitleHandler(
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IPlexSecretStore plexSecretStore,
        IPlexTelevisionLibraryScanner plexTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        ILogger<SynchronizePlexShowByTitleHandler> logger)
    {
        _mediator = mediator;
        _mediaSourceRepository = mediaSourceRepository;
        _plexSecretStore = plexSecretStore;
        _plexTelevisionLibraryScanner = plexTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizePlexShowByTitle request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request);
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

        _logger.LogInformation(
            "Starting targeted scan for show '{ShowTitle}' in Plex library {LibraryName}",
            parameters.ShowTitle,
            parameters.Library.Name);

        Either<BaseError, Unit> result = await _plexTelevisionLibraryScanner.ScanSingleShow(
            parameters.ConnectionParameters.ActiveConnection,
            parameters.ConnectionParameters.PlexServerAuthToken,
            parameters.Library,
            parameters.ShowTitle,
            parameters.DeepScan,
            cancellationToken);

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError("Error synchronizing Plex show '{ShowTitle}': {Error}", parameters.ShowTitle, error);
        }

        return result.Map(_ => $"Show '{parameters.ShowTitle}' in {parameters.Library.Name}");
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(SynchronizePlexShowByTitle request) =>
        (await ValidateConnection(request), await PlexLibraryMustExist(request))
        .Apply((connectionParameters, plexLibrary) =>
            new RequestParameters(
                connectionParameters,
                plexLibrary,
                request.ShowTitle,
                request.DeepScan
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizePlexShowByTitle request) =>
        PlexMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

    private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
        SynchronizePlexShowByTitle request) =>
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
        SynchronizePlexShowByTitle request) =>
        _mediaSourceRepository.GetPlexLibrary(request.PlexLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Plex library {request.PlexLibraryId} does not exist."));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        PlexLibrary Library,
        string ShowTitle,
        bool DeepScan);

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken? PlexServerAuthToken { get; set; }
    }
}