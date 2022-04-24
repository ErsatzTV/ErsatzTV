using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.MediaSources;

public class ScanLocalLibraryHandler : IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>,
    IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IEntityLocker _entityLocker;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<ScanLocalLibraryHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMovieFolderScanner _movieFolderScanner;
    private readonly IMusicVideoFolderScanner _musicVideoFolderScanner;
    private readonly IOtherVideoFolderScanner _otherVideoFolderScanner;
    private readonly ISongFolderScanner _songFolderScanner;
    private readonly ITelevisionFolderScanner _televisionFolderScanner;

    public ScanLocalLibraryHandler(
        ILibraryRepository libraryRepository,
        IConfigElementRepository configElementRepository,
        IMovieFolderScanner movieFolderScanner,
        ITelevisionFolderScanner televisionFolderScanner,
        IMusicVideoFolderScanner musicVideoFolderScanner,
        IOtherVideoFolderScanner otherVideoFolderScanner,
        ISongFolderScanner songFolderScanner,
        IEntityLocker entityLocker,
        IMediator mediator,
        ILogger<ScanLocalLibraryHandler> logger)
    {
        _libraryRepository = libraryRepository;
        _configElementRepository = configElementRepository;
        _movieFolderScanner = movieFolderScanner;
        _televisionFolderScanner = televisionFolderScanner;
        _musicVideoFolderScanner = musicVideoFolderScanner;
        _otherVideoFolderScanner = otherVideoFolderScanner;
        _songFolderScanner = songFolderScanner;
        _entityLocker = entityLocker;
        _mediator = mediator;
        _logger = logger;
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>.Handle(
        ForceScanLocalLibrary request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>.Handle(
        ScanLocalLibraryIfNeeded request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private Task<Either<BaseError, string>> Handle(IScanLocalLibrary request, CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(parameters => PerformScan(parameters, cancellationToken).Map(_ => parameters.LocalLibrary.Name))
            .Bind(v => v.ToEitherAsync());

    private async Task<Unit> PerformScan(RequestParameters parameters, CancellationToken cancellationToken)
    {
        (LocalLibrary localLibrary, string ffprobePath, string ffmpegPath, bool forceScan,
            int libraryRefreshInterval) = parameters;

        try
        {
            var sw = new Stopwatch();
            sw.Start();

            var scanned = false;

            for (var i = 0; i < localLibrary.Paths.Count; i++)
            {
                LibraryPath libraryPath = localLibrary.Paths[i];

                decimal progressMin = (decimal)i / localLibrary.Paths.Count;
                decimal progressMax = (decimal)(i + 1) / localLibrary.Paths.Count;

                var lastScan = new DateTimeOffset(libraryPath.LastScan ?? SystemTime.MinValueUtc, TimeSpan.Zero);
                DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
                if (forceScan || nextScan < DateTimeOffset.Now)
                {
                    scanned = true;

                    Either<BaseError, Unit> result = localLibrary.MediaKind switch
                    {
                        LibraryMediaKind.Movies =>
                            await _movieFolderScanner.ScanFolder(
                                libraryPath,
                                ffmpegPath,
                                ffprobePath,
                                progressMin,
                                progressMax,
                                cancellationToken),
                        LibraryMediaKind.Shows =>
                            await _televisionFolderScanner.ScanFolder(
                                libraryPath,
                                ffmpegPath,
                                ffprobePath,
                                progressMin,
                                progressMax,
                                cancellationToken),
                        LibraryMediaKind.MusicVideos =>
                            await _musicVideoFolderScanner.ScanFolder(
                                libraryPath,
                                ffmpegPath,
                                ffprobePath,
                                progressMin,
                                progressMax,
                                cancellationToken),
                        LibraryMediaKind.OtherVideos =>
                            await _otherVideoFolderScanner.ScanFolder(
                                libraryPath,
                                ffmpegPath,
                                ffprobePath,
                                progressMin,
                                progressMax,
                                cancellationToken),
                        LibraryMediaKind.Songs =>
                            await _songFolderScanner.ScanFolder(
                                libraryPath,
                                ffprobePath,
                                ffmpegPath,
                                progressMin,
                                progressMax,
                                cancellationToken),
                        _ => Unit.Default
                    };

                    if (result.IsRight)
                    {
                        libraryPath.LastScan = DateTime.UtcNow;
                        await _libraryRepository.UpdateLastScan(libraryPath);
                    }
                }

                await _mediator.Publish(new LibraryScanProgress(libraryPath.LibraryId, progressMax), cancellationToken);
            }

            sw.Stop();

            if (scanned)
            {
                _logger.LogDebug(
                    "Scan of library {Name} completed in {Duration}",
                    localLibrary.Name,
                    TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
            else
            {
                _logger.LogDebug(
                    "Skipping unforced scan of local media library {Name}",
                    localLibrary.Name);
            }

            await _mediator.Publish(new LibraryScanProgress(localLibrary.Id, 0), cancellationToken);

            return Unit.Default;
        }
        finally
        {
            _entityLocker.UnlockLibrary(localLibrary.Id);
        }
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(IScanLocalLibrary request)
    {
        Validation<BaseError, LocalLibrary> libraryResult = await LocalLibraryMustExist(request);
        Validation<BaseError, string> ffprobePathResult = await ValidateFFprobePath();
        Validation<BaseError, string> ffmpegPathResult = await ValidateFFmpegPath();
        Validation<BaseError, int> refreshIntervalResult = await ValidateLibraryRefreshInterval();

        try
        {
            return (libraryResult, ffprobePathResult, ffmpegPathResult, refreshIntervalResult)
                .Apply(
                    (library, ffprobePath, ffmpegPath, libraryRefreshInterval) => new RequestParameters(
                        library,
                        ffprobePath,
                        ffmpegPath,
                        request.ForceScan,
                        libraryRefreshInterval));
        }
        finally
        {
            // ensure we unlock the library if any validation is unsuccessful
            foreach (LocalLibrary library in libraryResult.SuccessToSeq())
            {
                if (ffprobePathResult.IsFail || ffmpegPathResult.IsFail || refreshIntervalResult.IsFail)
                {
                    _entityLocker.UnlockLibrary(library.Id);
                }
            }
        }
    }

    private Task<Validation<BaseError, LocalLibrary>> LocalLibraryMustExist(
        IScanLocalLibrary request) =>
        _libraryRepository.Get(request.LibraryId)
            .Map(maybeLibrary => maybeLibrary.Map(ms => ms as LocalLibrary))
            .Map(v => v.ToValidation<BaseError>($"Local library {request.LibraryId} does not exist."));

    private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
            .FilterT(File.Exists)
            .Map(
                ffprobePath =>
                    ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

    private Task<Validation<BaseError, string>> ValidateFFmpegPath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(
                ffmpegPath =>
                    ffmpegPath.ToValidation<BaseError>("FFmpeg path does not exist on the file system"));

    private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval() =>
        _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
            .FilterT(lri => lri > 0)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private record RequestParameters(
        LocalLibrary LocalLibrary,
        string FFprobePath,
        string FFmpegPath,
        bool ForceScan,
        int LibraryRefreshInterval);
}
