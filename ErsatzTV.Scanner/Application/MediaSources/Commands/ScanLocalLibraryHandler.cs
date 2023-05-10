using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Application.MediaSources;

public class ScanLocalLibraryHandler : IRequestHandler<ScanLocalLibrary, Either<BaseError, string>>
{
    private readonly IConfigElementRepository _configElementRepository;
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
        _mediator = mediator;
        _logger = logger;
    }

    public Task<Either<BaseError, string>> Handle(ScanLocalLibrary request, CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(parameters => PerformScan(parameters, cancellationToken).Map(_ => parameters.LocalLibrary.Name))
            .Bind(v => v.ToEitherAsync());

    private async Task<Unit> PerformScan(RequestParameters parameters, CancellationToken cancellationToken)
    {
        (LocalLibrary localLibrary, string ffprobePath, string ffmpegPath, bool forceScan,
            int libraryRefreshInterval) = parameters;

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
            if (forceScan || libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now)
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

            await _mediator.Publish(
                new ScannerProgressUpdate(
                    libraryPath.LibraryId,
                    localLibrary.Name,
                    progressMax,
                    Array.Empty<int>(),
                    Array.Empty<int>()),
                cancellationToken);
        }

        sw.Stop();

        if (scanned)
        {
            _logger.LogDebug(
                "Scan of library {Name} completed in {Duration}",
                localLibrary.Name,
                sw.Elapsed.Humanize());
        }
        else
        {
            _logger.LogDebug(
                "Skipping unforced scan of local media library {Name}",
                localLibrary.Name);
        }

        await _mediator.Publish(
            new ScannerProgressUpdate(localLibrary.Id, localLibrary.Name, 0, Array.Empty<int>(), Array.Empty<int>()),
            cancellationToken);

        return Unit.Default;
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(ScanLocalLibrary request)
    {
        Validation<BaseError, LocalLibrary> libraryResult = await LocalLibraryMustExist(request);
        Validation<BaseError, string> ffprobePathResult = await ValidateFFprobePath();
        Validation<BaseError, string> ffmpegPathResult = await ValidateFFmpegPath();
        Validation<BaseError, int> refreshIntervalResult = await ValidateLibraryRefreshInterval();

        return (libraryResult, ffprobePathResult, ffmpegPathResult, refreshIntervalResult)
            .Apply(
                (library, ffprobePath, ffmpegPath, libraryRefreshInterval) => new RequestParameters(
                    library,
                    ffprobePath,
                    ffmpegPath,
                    request.ForceScan,
                    libraryRefreshInterval));
    }

    private Task<Validation<BaseError, LocalLibrary>> LocalLibraryMustExist(ScanLocalLibrary request) =>
        _libraryRepository.Get(request.LibraryId)
            .Map(maybeLibrary => maybeLibrary.OfType<LocalLibrary>().HeadOrNone())
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
            .FilterT(lri => lri is >= 0 and < 1_000_000)
            .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

    private record RequestParameters(
        LocalLibrary LocalLibrary,
        string FFprobePath,
        string FFmpegPath,
        bool ForceScan,
        int LibraryRefreshInterval);
}
