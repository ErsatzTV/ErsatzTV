using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public class SynchronizeJellyfinShowByTitleHandler : IRequestHandler<SynchronizeJellyfinShowByTitle, Either<BaseError, string>>
{
    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IJellyfinTelevisionLibraryScanner _jellyfinTelevisionLibraryScanner;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizeJellyfinShowByTitleHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeJellyfinShowByTitleHandler(
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinTelevisionLibraryScanner jellyfinTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        ILogger<SynchronizeJellyfinShowByTitleHandler> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinTelevisionLibraryScanner = jellyfinTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizeJellyfinShowByTitle request,
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
            "Starting targeted scan for show '{ShowTitle}' in Jellyfin library {LibraryName}",
            parameters.ShowTitle,
            parameters.Library.Name);

        Either<BaseError, Unit> result = await _jellyfinTelevisionLibraryScanner.ScanSingleShow(
            parameters.ConnectionParameters.ActiveConnection.Address,
            parameters.ConnectionParameters.ApiKey,
            parameters.Library,
            parameters.ShowTitle,
            parameters.DeepScan,
            cancellationToken);

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError("Error synchronizing Jellyfin show '{ShowTitle}': {Error}", parameters.ShowTitle, error);
        }

        return result.Map(_ => $"Show '{parameters.ShowTitle}' in {parameters.Library.Name}");
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(SynchronizeJellyfinShowByTitle request) =>
        (await ValidateConnection(request), await JellyfinLibraryMustExist(request))
        .Apply((connectionParameters, jellyfinLibrary) =>
            new RequestParameters(
                connectionParameters,
                jellyfinLibrary,
                request.ShowTitle,
                request.DeepScan
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeJellyfinShowByTitle request) =>
        JellyfinMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist(
        SynchronizeJellyfinShowByTitle request) =>
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
        SynchronizeJellyfinShowByTitle request) =>
        _mediaSourceRepository.GetJellyfinLibrary(request.JellyfinLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Jellyfin library {request.JellyfinLibraryId} does not exist."));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        JellyfinLibrary Library,
        string ShowTitle,
        bool DeepScan);

    private record ConnectionParameters(JellyfinConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}