using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.MediaSources;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public class
    SynchronizeJellyfinLibraryByIdHandler : IRequestHandler<SynchronizeJellyfinLibraryById, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IJellyfinMovieLibraryScanner _jellyfinMovieLibraryScanner;

    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IJellyfinTelevisionLibraryScanner _jellyfinTelevisionLibraryScanner;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizeJellyfinLibraryByIdHandler> _logger;

    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IMediator _mediator;
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public SynchronizeJellyfinLibraryByIdHandler(
        IJellyfinApiClient jellyfinApiClient,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinMovieLibraryScanner jellyfinMovieLibraryScanner,
        IJellyfinTelevisionLibraryScanner jellyfinTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        IConfigElementRepository configElementRepository,
        ILogger<SynchronizeJellyfinLibraryByIdHandler> logger)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _mediator = mediator;
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinMovieLibraryScanner = jellyfinMovieLibraryScanner;
        _jellyfinTelevisionLibraryScanner = jellyfinTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>>
        Handle(SynchronizeJellyfinLibraryById request, CancellationToken cancellationToken)
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
            // need the jellyfin admin user id for now
            Either<BaseError, Unit> syncAdminResult = await _mediator.Send(
                new SynchronizeJellyfinAdminUserId(parameters.Library.MediaSourceId),
                cancellationToken);

            foreach (BaseError error in syncAdminResult.LeftToSeq())
            {
                _logger.LogError("Error synchronizing jellyfin admin user id: {Error}", error);
                return error;
            }

            Either<BaseError, Unit> result = parameters.Library.MediaKind switch
            {
                LibraryMediaKind.Movies =>
                    await _jellyfinMovieLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
                        parameters.Library,
                        parameters.DeepScan,
                        cancellationToken),
                LibraryMediaKind.Shows =>
                    await _jellyfinTelevisionLibraryScanner.ScanLibrary(
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

                // need to call get libraries to find library that contains collections (box sets)                
                await _jellyfinApiClient.GetLibraries(
                    parameters.ConnectionParameters.ActiveConnection.Address,
                    parameters.ConnectionParameters.ApiKey);

                Either<BaseError, Unit> collectionResult = await _mediator.Send(
                    new SynchronizeJellyfinCollections(parameters.Library.MediaSourceId),
                    cancellationToken);

                collectionResult.BiIter(
                    _ => _logger.LogDebug("Done synchronizing jellyfin collections"),
                    error => _logger.LogWarning(
                        "Unable to synchronize jellyfin collections for source {MediaSourceId}: {Error}",
                        parameters.Library.MediaSourceId,
                        error.Value));
            }

            foreach (BaseError error in result.LeftToSeq())
            {
                _logger.LogError("Error synchronizing jellyfin library: {Error}", error);
            }

            return result.Map(_ => parameters.Library.Name);
        }

        _logger.LogDebug("Skipping unforced scan of jellyfin media library {Name}", parameters.Library.Name);

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
        SynchronizeJellyfinLibraryById request) =>
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
                    ffprobePath,
                    request.DeepScan
                ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        SynchronizeJellyfinLibraryById request) =>
        JellyfinMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist(
        SynchronizeJellyfinLibraryById request) =>
        _mediaSourceRepository.GetJellyfinByLibraryId(request.JellyfinLibraryId)
            .Map(
                v => v.ToValidation<BaseError>(
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
        SynchronizeJellyfinLibraryById request) =>
        _mediaSourceRepository.GetJellyfinLibrary(request.JellyfinLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Jellyfin library {request.JellyfinLibraryId} does not exist."));

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
        JellyfinLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        string FFmpegPath,
        string FFprobePath,
        bool DeepScan);

    private record ConnectionParameters(JellyfinConnection ActiveConnection)
    {
        public string? ApiKey { get; init; }
    }
}
