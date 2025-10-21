using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Emby;

public class SynchronizeEmbyShowByIdHandler : IRequestHandler<SynchronizeEmbyShowById, Either<BaseError, string>>
{
    private readonly IEmbySecretStore _embySecretStore;
    private readonly IEmbyTelevisionLibraryScanner _embyTelevisionLibraryScanner;
    private readonly IEmbyTelevisionRepository _embyTelevisionRepository;
    private readonly ILogger<SynchronizeEmbyShowByIdHandler> _logger;
    private readonly IScannerProxy _scannerProxy;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyShowByIdHandler(
        IScannerProxy scannerProxy,
        IMediaSourceRepository mediaSourceRepository,
        IEmbyTelevisionRepository embyTelevisionRepository,
        IEmbySecretStore embySecretStore,
        IEmbyTelevisionLibraryScanner embyTelevisionLibraryScanner,
        ILogger<SynchronizeEmbyShowByIdHandler> logger)
    {
        _scannerProxy = scannerProxy;
        _mediaSourceRepository = mediaSourceRepository;
        _embyTelevisionRepository = embyTelevisionRepository;
        _embySecretStore = embySecretStore;
        _embyTelevisionLibraryScanner = embyTelevisionLibraryScanner;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> Handle(
        SynchronizeEmbyShowById request,
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
            "Starting targeted scan for show '{ShowTitle}' in Emby library {LibraryName}",
            parameters.ShowTitle,
            parameters.Library.Name);

        Either<BaseError, Unit> result = await _embyTelevisionLibraryScanner.ScanSingleShow(
            parameters.ConnectionParameters.ActiveConnection.Address,
            parameters.ConnectionParameters.ApiKey,
            parameters.Library,
            parameters.ItemId,
            parameters.ShowTitle,
            parameters.DeepScan,
            cancellationToken);

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError("Error synchronizing Emby show '{ShowTitle}': {Error}", parameters.ShowTitle, error);
        }

        return result.Map(_ => $"Show '{parameters.ShowTitle}' in {parameters.Library.Name}");
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizeEmbyShowById request,
        CancellationToken cancellationToken) =>
        (await ValidateConnection(request), await EmbyLibraryMustExist(request, cancellationToken),
            await EmbyShowMustExist(request, cancellationToken))
        .Apply((connectionParameters, embyLibrary, showTitleItemId) =>
            new RequestParameters(
                connectionParameters,
                embyLibrary,
                showTitleItemId.ItemId,
                showTitleItemId.Title,
                request.DeepScan,
                request.BaseUrl
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeEmbyShowById request) =>
        EmbyMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
        SynchronizeEmbyShowById request) =>
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
        SynchronizeEmbyShowById request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetEmbyLibrary(request.EmbyLibraryId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>($"Emby library {request.EmbyLibraryId} does not exist."));

    private Task<Validation<BaseError, EmbyShowTitleItemIdResult>> EmbyShowMustExist(
        SynchronizeEmbyShowById request,
        CancellationToken cancellationToken) =>
        _embyTelevisionRepository.GetShowTitleItemId(request.EmbyLibraryId, request.ShowId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>(
                $"Jellyfin show {request.ShowId} does not exist in library {request.EmbyLibraryId}."));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        EmbyLibrary Library,
        string ItemId,
        string ShowTitle,
        bool DeepScan,
        string BaseUrl);

    private record ConnectionParameters(EmbyConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}
