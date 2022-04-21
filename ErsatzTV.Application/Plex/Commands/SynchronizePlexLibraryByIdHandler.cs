using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Plex;

public class
    SynchronizePlexLibraryByIdHandler : IRequestHandler<ForceSynchronizePlexLibraryById, Either<BaseError, string>>,
        IRequestHandler<SynchronizePlexLibraryByIdIfNeeded, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IEntityLocker _entityLocker;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<SynchronizePlexLibraryByIdHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexMovieLibraryScanner _plexMovieLibraryScanner;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexTelevisionLibraryScanner _plexTelevisionLibraryScanner;

    public SynchronizePlexLibraryByIdHandler(
        IMediaSourceRepository mediaSourceRepository,
        IConfigElementRepository configElementRepository,
        IPlexSecretStore plexSecretStore,
        IPlexMovieLibraryScanner plexMovieLibraryScanner,
        IPlexTelevisionLibraryScanner plexTelevisionLibraryScanner,
        ILibraryRepository libraryRepository,
        IEntityLocker entityLocker,
        ILogger<SynchronizePlexLibraryByIdHandler> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _configElementRepository = configElementRepository;
        _plexSecretStore = plexSecretStore;
        _plexMovieLibraryScanner = plexMovieLibraryScanner;
        _plexTelevisionLibraryScanner = plexTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _entityLocker = entityLocker;
        _logger = logger;
    }

    public Task<Either<BaseError, string>> Handle(
        ForceSynchronizePlexLibraryById request,
        CancellationToken cancellationToken) => Handle(request);

    public Task<Either<BaseError, string>> Handle(
        SynchronizePlexLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => Handle(request);

    private Task<Either<BaseError, string>>
        Handle(ISynchronizePlexLibraryById request) =>
        Validate(request)
            .MapT(parameters => Synchronize(parameters).Map(_ => parameters.Library.Name))
            .Bind(v => v.ToEitherAsync());

    private async Task<Unit> Synchronize(RequestParameters parameters)
    {
        try
        {
            var lastScan = new DateTimeOffset(parameters.Library.LastScan ?? SystemTime.MinValueUtc, TimeSpan.Zero);
            DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
            if (parameters.ForceScan || nextScan < DateTimeOffset.Now)
            {
                switch (parameters.Library.MediaKind)
                {
                    case LibraryMediaKind.Movies:
                        await _plexMovieLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection,
                            parameters.ConnectionParameters.PlexServerAuthToken,
                            parameters.Library,
                            parameters.FFmpegPath,
                            parameters.FFprobePath,
                            parameters.DeepScan);
                        break;
                    case LibraryMediaKind.Shows:
                        await _plexTelevisionLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection,
                            parameters.ConnectionParameters.PlexServerAuthToken,
                            parameters.Library,
                            parameters.FFmpegPath,
                            parameters.FFprobePath,
                            parameters.DeepScan);
                        break;
                }

                parameters.Library.LastScan = DateTime.UtcNow;
                await _libraryRepository.UpdateLastScan(parameters.Library);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping unforced scan of plex media library {Name}",
                    parameters.Library.Name);
            }

            return Unit.Default;
        }
        finally
        {
            _entityLocker.UnlockLibrary(parameters.Library.Id);
        }
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(ISynchronizePlexLibraryById request) =>
        (await ValidateConnection(request), await PlexLibraryMustExist(request),
            await ValidateLibraryRefreshInterval(), await ValidateFFmpegPath(), await ValidateFFprobePath())
        .Apply(
            (connectionParameters, plexLibrary, libraryRefreshInterval, ffmpegPath, ffprobePath) =>
                new RequestParameters(
                    connectionParameters,
                    plexLibrary,
                    request.ForceScan,
                    libraryRefreshInterval,
                    ffmpegPath,
                    ffprobePath,
                    request.DeepScan
                ));

    private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
        ISynchronizePlexLibraryById request) =>
        PlexMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

    private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
        ISynchronizePlexLibraryById request) =>
        _mediaSourceRepository.GetPlexByLibraryId(request.PlexLibraryId)
            .Map(
                v => v.ToValidation<BaseError>(
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
        ISynchronizePlexLibraryById request) =>
        _mediaSourceRepository.GetPlexLibrary(request.PlexLibraryId)
            .Map(v => v.ToValidation<BaseError>($"Plex library {request.PlexLibraryId} does not exist."));

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
        PlexLibrary Library,
        bool ForceScan,
        int LibraryRefreshInterval,
        string FFmpegPath,
        string FFprobePath,
        bool DeepScan);

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken PlexServerAuthToken { get; set; }
    }
}
