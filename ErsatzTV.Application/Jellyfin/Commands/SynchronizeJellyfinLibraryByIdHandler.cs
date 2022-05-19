using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Jellyfin;

public class SynchronizeJellyfinLibraryByIdHandler :
    IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IEntityLocker _entityLocker;
    private readonly IJellyfinMovieLibraryScanner _jellyfinMovieLibraryScanner;

    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IJellyfinTelevisionLibraryScanner _jellyfinTelevisionLibraryScanner;
    private readonly ChannelWriter<IJellyfinBackgroundServiceRequest> _jellyfinWorkerChannel;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizeJellyfinLibraryByIdHandler> _logger;

    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeJellyfinLibraryByIdHandler(
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinMovieLibraryScanner jellyfinMovieLibraryScanner,
        IJellyfinTelevisionLibraryScanner jellyfinTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        IEntityLocker entityLocker,
        IConfigElementRepository configElementRepository,
        ChannelWriter<IJellyfinBackgroundServiceRequest> jellyfinWorkerChannel,
        ILogger<SynchronizeJellyfinLibraryByIdHandler> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinMovieLibraryScanner = jellyfinMovieLibraryScanner;
        _jellyfinTelevisionLibraryScanner = jellyfinTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _entityLocker = entityLocker;
        _configElementRepository = configElementRepository;
        _jellyfinWorkerChannel = jellyfinWorkerChannel;
        _logger = logger;
    }

    public Task<Either<BaseError, string>> Handle(
        ForceSynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken) => HandleImpl(request, cancellationToken);

    public Task<Either<BaseError, string>> Handle(
        SynchronizeJellyfinLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => HandleImpl(request, cancellationToken);

    private async Task<Either<BaseError, string>>
        HandleImpl(ISynchronizeJellyfinLibraryById request, CancellationToken cancellationToken)
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
            if (parameters.ForceScan || nextScan < DateTimeOffset.Now)
            {
                Either<BaseError, Unit> result = parameters.Library.MediaKind switch
                {
                    LibraryMediaKind.Movies =>
                        await _jellyfinMovieLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection.Address,
                            parameters.ConnectionParameters.ApiKey,
                            parameters.Library,
                            parameters.FFmpegPath,
                            parameters.FFprobePath,
                            cancellationToken),
                    LibraryMediaKind.Shows =>
                        await _jellyfinTelevisionLibraryScanner.ScanLibrary(
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

                    await _jellyfinWorkerChannel.WriteAsync(
                        new SynchronizeJellyfinCollections(parameters.Library.MediaSourceId),
                        cancellationToken);
                }

                return result.Map(_ => parameters.Library.Name);
            }
            else
            {
                _logger.LogDebug("Skipping unforced scan of jellyfin media library {Name}", parameters.Library.Name);
            }

            return parameters.Library.Name;
        }
        finally
        {
            _entityLocker.UnlockLibrary(parameters.Library.Id);
        }
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        ISynchronizeJellyfinLibraryById request) =>
        (await ValidateConnection(request), await JellyfinLibraryMustExist(request),
            await ValidateLibraryRefreshInterval(), await ValidateFFmpegPath(), await ValidateFFprobePath())
        .Apply(
            (connectionParameters, jellyfinLibrary, libraryRefreshInterval, ffmpegPath, ffprobePath) =>
                new RequestParameters(
                    connectionParameters,
                    jellyfinLibrary,
                    request.ForceScan,
                    libraryRefreshInterval,
                    ffmpegPath,
                    ffprobePath
                ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        ISynchronizeJellyfinLibraryById request) =>
        JellyfinMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist(
        ISynchronizeJellyfinLibraryById request) =>
        _mediaSourceRepository.GetJellyfinByLibraryId(request.JellyfinLibraryId)
            .Map(
                v => v.ToValidation<BaseError>(
                    $"Jellyfin media source for library {request.JellyfinLibraryId} does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        JellyfinMediaSource jellyfinMediaSource)
    {
        Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(jellyfinMediaSource, connection))
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
        ISynchronizeJellyfinLibraryById request) =>
        _mediaSourceRepository.GetJellyfinLibrary(request.JellyfinLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Jellyfin library {request.JellyfinLibraryId} does not exist."));

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval() =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
            .FilterT(lri => lri > 0)
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
        JellyfinLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        string FFmpegPath,
        string FFprobePath);

    private record ConnectionParameters(
        JellyfinMediaSource JellyfinMediaSource,
        JellyfinConnection ActiveConnection)
    {
        public string ApiKey { get; set; }
    }
}
