using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Emby;

public class SynchronizeEmbyLibraryByIdHandler :
    IRequestHandler<ForceSynchronizeEmbyLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeEmbyLibraryByIdIfNeeded, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IEmbyMovieLibraryScanner _embyMovieLibraryScanner;

    private readonly IEmbySecretStore _embySecretStore;
    private readonly IEmbyTelevisionLibraryScanner _embyTelevisionLibraryScanner;
    private readonly ChannelWriter<IEmbyBackgroundServiceRequest> _embyWorkerChannel;
    private readonly IEntityLocker _entityLocker;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizeEmbyLibraryByIdHandler> _logger;

    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeEmbyLibraryByIdHandler(
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore,
        IEmbyMovieLibraryScanner embyMovieLibraryScanner,
        IEmbyTelevisionLibraryScanner embyTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        IEntityLocker entityLocker,
        IConfigElementRepository configElementRepository,
        ChannelWriter<IEmbyBackgroundServiceRequest> embyWorkerChannel,
        ILogger<SynchronizeEmbyLibraryByIdHandler> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _embySecretStore = embySecretStore;
        _embyMovieLibraryScanner = embyMovieLibraryScanner;
        _embyTelevisionLibraryScanner = embyTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _entityLocker = entityLocker;
        _configElementRepository = configElementRepository;
        _embyWorkerChannel = embyWorkerChannel;
        _logger = logger;
    }

    public Task<Either<BaseError, string>> Handle(
        ForceSynchronizeEmbyLibraryById request,
        CancellationToken cancellationToken) => HandleImpl(request, cancellationToken);

    public Task<Either<BaseError, string>> Handle(
        SynchronizeEmbyLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => HandleImpl(request, cancellationToken);

    private async Task<Either<BaseError, string>>
        HandleImpl(ISynchronizeEmbyLibraryById request, CancellationToken cancellationToken)
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
        try
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

                    await _embyWorkerChannel.WriteAsync(
                        new SynchronizeEmbyCollections(parameters.Library.MediaSourceId),
                        cancellationToken);
                }

                return result.Map(_ => parameters.Library.Name);
            }
            else
            {
                _logger.LogDebug("Skipping unforced scan of emby media library {Name}", parameters.Library.Name);
            }

            return parameters.Library.Name;
        }
        finally
        {
            _entityLocker.UnlockLibrary(parameters.Library.Id);
        }
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        ISynchronizeEmbyLibraryById request) =>
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
        ISynchronizeEmbyLibraryById request) =>
        EmbyMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
        ISynchronizeEmbyLibraryById request) =>
        _mediaSourceRepository.GetEmbyByLibraryId(request.EmbyLibraryId)
            .Map(
                v => v.ToValidation<BaseError>(
                    $"Emby media source for library {request.EmbyLibraryId} does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        EmbyMediaSource embyMediaSource)
    {
        Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(embyMediaSource, connection))
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
        ISynchronizeEmbyLibraryById request) =>
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

    private record ConnectionParameters(
        EmbyMediaSource EmbyMediaSource,
        EmbyConnection ActiveConnection)
    {
        public string ApiKey { get; set; }
    }
}
