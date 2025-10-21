using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public class
    SynchronizeJellyfinShowByIdHandler : IRequestHandler<SynchronizeJellyfinShowById, Either<BaseError, string>>
{
    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IJellyfinTelevisionLibraryScanner _jellyfinTelevisionLibraryScanner;
    private readonly IJellyfinTelevisionRepository _jellyfinTelevisionRepository;
    private readonly ILogger<SynchronizeJellyfinShowByIdHandler> _logger;
    private readonly IScannerProxy _scannerProxy;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeJellyfinShowByIdHandler(
        IScannerProxy scannerProxy,
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinTelevisionRepository jellyfinTelevisionRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinTelevisionLibraryScanner jellyfinTelevisionLibraryScanner,
        ILogger<SynchronizeJellyfinShowByIdHandler> logger)
    {
        _scannerProxy = scannerProxy;
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinTelevisionRepository = jellyfinTelevisionRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinTelevisionLibraryScanner = jellyfinTelevisionLibraryScanner;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizeJellyfinShowById request,
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
            "Starting targeted scan for show '{ShowTitle}' in Jellyfin library {LibraryName}",
            parameters.ShowTitle,
            parameters.Library.Name);

        Either<BaseError, Unit> result = await _jellyfinTelevisionLibraryScanner.ScanSingleShow(
            parameters.ConnectionParameters.ActiveConnection.Address,
            parameters.ConnectionParameters.ApiKey,
            parameters.Library,
            parameters.ItemId,
            parameters.ShowTitle,
            parameters.DeepScan,
            cancellationToken);

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError("Error synchronizing Jellyfin show '{ShowTitle}': {Error}", parameters.ShowTitle, error);
        }

        return result.Map(_ => $"Show '{parameters.ShowTitle}' in {parameters.Library.Name}");
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizeJellyfinShowById request,
        CancellationToken cancellationToken) =>
        (await ValidateConnection(request), await JellyfinLibraryMustExist(request),
            await JellyfinShowMustExist(request, cancellationToken))
        .Apply((connectionParameters, jellyfinLibrary, showTitleItemId) =>
            new RequestParameters(
                connectionParameters,
                jellyfinLibrary,
                showTitleItemId.ItemId,
                showTitleItemId.Title,
                request.DeepScan,
                request.BaseUrl
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeJellyfinShowById request) =>
        JellyfinMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist(
        SynchronizeJellyfinShowById request) =>
        _mediaSourceRepository.GetJellyfinByLibraryId(request.JellyfinLibraryId)
            .Map(v => v.ToValidation<BaseError>(
                $"Jellyfin media source for library {request.JellyfinLibraryId} does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        JellyfinMediaSource jellyfinMediaSource)
    {
        Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(connection))
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

    private Task<Validation<BaseError, JellyfinLibrary>> JellyfinLibraryMustExist(
        SynchronizeJellyfinShowById request) =>
        _mediaSourceRepository.GetJellyfinLibrary(request.JellyfinLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Jellyfin library {request.JellyfinLibraryId} does not exist."));

    private Task<Validation<BaseError, JellyfinShowTitleItemIdResult>> JellyfinShowMustExist(
        SynchronizeJellyfinShowById request,
        CancellationToken cancellationToken) =>
        _jellyfinTelevisionRepository.GetShowTitleItemId(request.JellyfinLibraryId, request.ShowId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>(
                $"Jellyfin show {request.ShowId} does not exist in library {request.JellyfinLibraryId}."));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        JellyfinLibrary Library,
        string ItemId,
        string ShowTitle,
        bool DeepScan,
        string BaseUrl);

    private record ConnectionParameters(JellyfinConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}
