using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Emby;

public class SynchronizeEmbyLibraryByIdHandler : IRequestHandler<SynchronizeEmbyLibraryById, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IEmbyMovieLibraryScanner _embyMovieLibraryScanner;

    private readonly IEmbySecretStore _embySecretStore;
    private readonly IEmbyTelevisionLibraryScanner _embyTelevisionLibraryScanner;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizeEmbyLibraryByIdHandler> _logger;
    private readonly IScannerProxy _scannerProxy;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyLibraryByIdHandler(
        IScannerProxy scannerProxy,
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore,
        IEmbyMovieLibraryScanner embyMovieLibraryScanner,
        IEmbyTelevisionLibraryScanner embyTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        IConfigElementRepository configElementRepository,
        ILogger<SynchronizeEmbyLibraryByIdHandler> logger)
    {
        _scannerProxy = scannerProxy;
        _mediaSourceRepository = mediaSourceRepository;
        _embySecretStore = embySecretStore;
        _embyMovieLibraryScanner = embyMovieLibraryScanner;
        _embyTelevisionLibraryScanner = embyTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>>
        Handle(SynchronizeEmbyLibraryById request, CancellationToken cancellationToken)
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
                    await _embyMovieLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
                        parameters.Library,
                        parameters.DeepScan,
                        cancellationToken),
                LibraryMediaKind.Shows =>
                    await _embyTelevisionLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
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
                _logger.LogError("Error synchronizing emby library: {Error}", error);
            }

            return result.Map(_ => parameters.Library.Name);
        }

        _logger.LogDebug("Skipping unforced scan of emby media library {Name}", parameters.Library.Name);

        return parameters.Library.Name;
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizeEmbyLibraryById request,
        CancellationToken cancellationToken) =>
        (await ValidateConnection(request), await EmbyLibraryMustExist(request, cancellationToken),
            await ValidateLibraryRefreshInterval(cancellationToken))
        .Apply((connectionParameters, embyLibrary, libraryRefreshInterval) =>
            new RequestParameters(
                connectionParameters,
                embyLibrary,
                request.ForceScan,
                libraryRefreshInterval,
                request.DeepScan,
                request.BaseUrl
            ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeEmbyLibraryById request) =>
        EmbyMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
        SynchronizeEmbyLibraryById request) =>
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
        SynchronizeEmbyLibraryById request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetEmbyLibrary(request.EmbyLibraryId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>($"Emby library {request.EmbyLibraryId} does not exist."));

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval(CancellationToken cancellationToken) =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval, cancellationToken)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        EmbyLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        bool DeepScan,
        string BaseUrl);

    private record ConnectionParameters(EmbyConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}
