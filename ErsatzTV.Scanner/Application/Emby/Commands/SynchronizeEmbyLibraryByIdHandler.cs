using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
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

    private readonly IEmbyApiClient _embyApiClient;
    private readonly IMediator _mediator;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyLibraryByIdHandler(
        IEmbyApiClient embyApiClient,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore,
        IEmbyMovieLibraryScanner embyMovieLibraryScanner,
        IEmbyTelevisionLibraryScanner embyTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        IConfigElementRepository configElementRepository,
        ILogger<SynchronizeEmbyLibraryByIdHandler> logger)
    {
        _embyApiClient = embyApiClient;
        _mediator = mediator;
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
        Validation<BaseError, RequestParameters> validation = await Validate(request);
        return await validation.Match(
            parameters => Synchronize(parameters, cancellationToken),
            error => Task.FromResult<Either<BaseError, string>>(error.Join()));
    }

    private async Task<Either<BaseError, string>> Synchronize(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        var lastScan = new DateTimeOffset(parameters.Library.LastScan ?? SystemTime.MinValueUtc, TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || (parameters.LibraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now))
        {
            Either<BaseError, Unit> result = parameters.Library.MediaKind switch
            {
                LibraryMediaKind.Movies =>
                    await _embyMovieLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
                        parameters.Library,
                        parameters.FFmpegPath,
                        parameters.FFprobePath,
                        cancellationToken),
                LibraryMediaKind.Shows =>
                    await _embyTelevisionLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
                        parameters.Library,
                        parameters.FFmpegPath,
                        parameters.FFprobePath,
                        cancellationToken),
                _ => Unit.Default
            };

            if (result.IsRight)
            {
                parameters.Library.LastScan = DateTime.UtcNow;
                await _libraryRepository.UpdateLastScan(parameters.Library);

                // need to call get libraries to find library that contains collections (box sets)                
                await _embyApiClient.GetLibraries(
                    parameters.ConnectionParameters.ActiveConnection.Address,
                    parameters.ConnectionParameters.ApiKey);

                Either<BaseError, Unit> collectionResult = await _mediator.Send(
                    new SynchronizeEmbyCollections(parameters.Library.MediaSourceId),
                    cancellationToken);

                collectionResult.BiIter(
                    _ => _logger.LogDebug("Done synchronizing emby collections"),
                    error => _logger.LogWarning(
                        "Unable to synchronize emby collections for source {MediaSourceId}: {Error}",
                        parameters.Library.MediaSourceId,
                        error.Value));
            }
            
            foreach (BaseError error in result.LeftToSeq())
            {
                _logger.LogError("Error synchronizing emby library: {Error}", error);
            }

            return result.Map(_ => parameters.Library.Name);
        }

        _logger.LogDebug("Skipping unforced scan of emby media library {Name}", parameters.Library.Name);

        // send an empty progress update for the library name
        await _mediator.Publish(
            new ScannerProgressUpdate(
                parameters.Library.Id,
                parameters.Library.Name,
                0,
                Array.Empty<int>(),
                Array.Empty<int>()),
            cancellationToken);

        return parameters.Library.Name;
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        SynchronizeEmbyLibraryById request) =>
        (await ValidateConnection(request), await EmbyLibraryMustExist(request),
            await ValidateLibraryRefreshInterval(), await ValidateFFmpegPath(), await ValidateFFprobePath())
        .Apply(
            (connectionParameters, embyLibrary, libraryRefreshInterval, ffmpegPath, ffprobePath) =>
                new RequestParameters(
                    connectionParameters,
                    embyLibrary,
                    request.ForceScan,
                    libraryRefreshInterval,
                    ffmpegPath,
                    ffprobePath
                ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeEmbyLibraryById request) =>
        EmbyMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
        SynchronizeEmbyLibraryById request) =>
        _mediaSourceRepository.GetEmbyByLibraryId(request.EmbyLibraryId)
            .Map(
                v => v.ToValidation<BaseError>(
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
        SynchronizeEmbyLibraryById request) =>
        _mediaSourceRepository.GetEmbyLibrary(request.EmbyLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Emby library {request.EmbyLibraryId} does not exist."));

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval() =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private Task<Validation<BaseError, string>> ValidateFFmpegPath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(
                ffmpegPath =>
                    ffmpegPath.ToValidation<BaseError>("FFmpeg path does not exist on the file system"));

    private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
            .FilterT(File.Exists)
            .Map(
                ffprobePath =>
                    ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

    private record RequestParameters(
        ConnectionParameters ConnectionParameters,
        EmbyLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        string FFmpegPath,
        string FFprobePath);

    private record ConnectionParameters(EmbyConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}
