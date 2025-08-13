using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Emby;

public class SynchronizeEmbyShowByTitleHandler : IRequestHandler<SynchronizeEmbyShowByTitle, Either<BaseError, string>>
{
    private readonly IEmbySecretStore _embySecretStore;
    private readonly IEmbyTelevisionLibraryScanner _embyTelevisionLibraryScanner;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizeEmbyShowByTitleHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyShowByTitleHandler(
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore,
        IEmbyTelevisionLibraryScanner embyTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        ILogger<SynchronizeEmbyShowByTitleHandler> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _embySecretStore = embySecretStore;
        _embyTelevisionLibraryScanner = embyTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizeEmbyShowByTitle request,
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
            "Starting targeted scan for show '{ShowTitle}' in Emby library {LibraryName}",
            parameters.ShowTitle,
            parameters.Library.Name);

        Either<BaseError, Unit> result = await _embyTelevisionLibraryScanner.ScanSingleShow(
            parameters.ConnectionParameters.ActiveConnection.Address,
            parameters.ConnectionParameters.ApiKey,
            parameters.Library,
            parameters.ShowTitle,
            parameters.DeepScan,
            cancellationToken);

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError("Error synchronizing Emby show '{ShowTitle}': {Error}", parameters.ShowTitle, error);
        }

        return result.Map(_ => $"Show '{parameters.ShowTitle}' in {parameters.Library.Name}");
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(SynchronizeEmbyShowByTitle request) =>
        (await ValidateConnection(request), await EmbyLibraryMustExist(request))
        .Apply((connectionParameters, embyLibrary) =>
            new RequestParameters(
                connectionParameters,
                embyLibrary,
                request.ShowTitle,
                request.DeepScan
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeEmbyShowByTitle request) =>
        EmbyMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
        SynchronizeEmbyShowByTitle request) =>
        _mediaSourceRepository.GetEmbyByLibraryId(request.EmbyLibraryId)
            .Map(v => v.ToValidation<BaseError>(
                $"Emby media source for library {request.EmbyLibraryId} does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        EmbyMediaSource embyMediaSource)
    {
        Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(connection))
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

    private Task<Validation<BaseError, EmbyLibrary>> EmbyLibraryMustExist(
        SynchronizeEmbyShowByTitle request) =>
        _mediaSourceRepository.GetEmbyLibrary(request.EmbyLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Emby library {request.EmbyLibraryId} does not exist."));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        EmbyLibrary Library,
        string ShowTitle,
        bool DeepScan);

    private record ConnectionParameters(EmbyConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}