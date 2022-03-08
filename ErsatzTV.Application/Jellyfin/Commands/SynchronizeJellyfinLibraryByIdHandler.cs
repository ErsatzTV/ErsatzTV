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
        ILogger<SynchronizeJellyfinLibraryByIdHandler> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinMovieLibraryScanner = jellyfinMovieLibraryScanner;
        _jellyfinTelevisionLibraryScanner = jellyfinTelevisionLibraryScanner;
        _libraryRepository = libraryRepository;
        _entityLocker = entityLocker;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public Task<Either<BaseError, string>> Handle(
        ForceSynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken) => Handle(request);

    public Task<Either<BaseError, string>> Handle(
        SynchronizeJellyfinLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => Handle(request);

    private Task<Either<BaseError, string>>
        Handle(ISynchronizeJellyfinLibraryById request) =>
        Validate(request)
            .MapT(parameters => Synchronize(parameters).Map(_ => parameters.Library.Name))
            .Bind(v => v.ToEitherAsync());

    private async Task<Unit> Synchronize(RequestParameters parameters)
    {
        var lastScan = new DateTimeOffset(parameters.Library.LastScan ?? SystemTime.MinValueUtc, TimeSpan.Zero);
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
        if (parameters.ForceScan || nextScan < DateTimeOffset.Now)
        {
            switch (parameters.Library.MediaKind)
            {
                case LibraryMediaKind.Movies:
                    await _jellyfinMovieLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
                        parameters.Library,
                        parameters.FFprobePath);
                    break;
                case LibraryMediaKind.Shows:
                    await _jellyfinTelevisionLibraryScanner.ScanLibrary(
                        parameters.ConnectionParameters.ActiveConnection.Address,
                        parameters.ConnectionParameters.ApiKey,
                        parameters.Library,
                        parameters.FFprobePath);
                    break;
            }

            parameters.Library.LastScan = DateTime.UtcNow;
            await _libraryRepository.UpdateLastScan(parameters.Library);
        }
        else
        {
            _logger.LogDebug(
                "Skipping unforced scan of jellyfin media library {Name}",
                parameters.Library.Name);
        }

        _entityLocker.UnlockLibrary(parameters.Library.Id);
        return Unit.Default;
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        ISynchronizeJellyfinLibraryById request) =>
        (await ValidateConnection(request), await JellyfinLibraryMustExist(request),
            await ValidateLibraryRefreshInterval(), await ValidateFFprobePath())
        .Apply(
            (connectionParameters, jellyfinLibrary, libraryRefreshInterval, ffprobePath) => new RequestParameters(
                connectionParameters,
                jellyfinLibrary,
                request.ForceScan,
                libraryRefreshInterval,
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
        string FFprobePath);

    private record ConnectionParameters(
        JellyfinMediaSource JellyfinMediaSource,
        JellyfinConnection ActiveConnection)
    {
        public string ApiKey { get; set; }
    }
}