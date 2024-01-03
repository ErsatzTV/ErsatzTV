using System.Diagnostics;
using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Streaming;

public class StartFFmpegSessionHandler : IRequestHandler<StartFFmpegSession, Either<BaseError, Unit>>
{
    private readonly IClient _client;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly IHlsPlaylistFilter _hlsPlaylistFilter;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<StartFFmpegSessionHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<HlsSessionWorker> _sessionWorkerLogger;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public StartFFmpegSessionHandler(
        IHlsPlaylistFilter hlsPlaylistFilter,
        IServiceScopeFactory serviceScopeFactory,
        IMediator mediator,
        IClient client,
        ILocalFileSystem localFileSystem,
        ILogger<StartFFmpegSessionHandler> logger,
        ILogger<HlsSessionWorker> sessionWorkerLogger,
        IFFmpegSegmenterService ffmpegSegmenterService,
        IConfigElementRepository configElementRepository,
        IHostApplicationLifetime hostApplicationLifetime,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _hlsPlaylistFilter = hlsPlaylistFilter;
        _serviceScopeFactory = serviceScopeFactory;
        _mediator = mediator;
        _client = client;
        _localFileSystem = localFileSystem;
        _logger = logger;
        _sessionWorkerLogger = sessionWorkerLogger;
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _configElementRepository = configElementRepository;
        _hostApplicationLifetime = hostApplicationLifetime;
        _workerChannel = workerChannel;
    }

    public Task<Either<BaseError, Unit>> Handle(StartFFmpegSession request, CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(_ => StartProcess(request, cancellationToken))
            // this weirdness is needed to maintain the error type (.ToEitherAsync() just gives BaseError)
#pragma warning disable VSTHRD103
            .Bind(v => v.ToEither().MapLeft(seq => seq.Head()).MapAsync<BaseError, Task<Unit>, Unit>(identity));
#pragma warning restore VSTHRD103

    private async Task<Unit> StartProcess(StartFFmpegSession request, CancellationToken cancellationToken)
    {
        TimeSpan idleTimeout = await _configElementRepository
            .GetValue<int>(ConfigElementKey.FFmpegSegmenterTimeout)
            .Map(maybeTimeout => maybeTimeout.Match(i => TimeSpan.FromSeconds(i), () => TimeSpan.FromMinutes(1)));

        Option<int> targetFramerate = await _mediator.Send(
            new GetChannelFramerate(request.ChannelNumber),
            cancellationToken);

        var worker = new HlsSessionWorker(
            _serviceScopeFactory,
            _client,
            _hlsPlaylistFilter,
            _configElementRepository,
            _localFileSystem,
            _sessionWorkerLogger,
            targetFramerate);
        _ffmpegSegmenterService.SessionWorkers.AddOrUpdate(request.ChannelNumber, _ => worker, (_, _) => worker);

        // fire and forget worker
        _ = worker.Run(request.ChannelNumber, idleTimeout, _hostApplicationLifetime.ApplicationStopping)
            .ContinueWith(
                _ =>
                {
                    _ffmpegSegmenterService.SessionWorkers.TryRemove(
                        request.ChannelNumber,
                        out IHlsSessionWorker inactiveWorker);

                    inactiveWorker?.Dispose();

                    _workerChannel.TryWrite(new ReleaseMemory(false));
                },
                TaskScheduler.Default);

        string playlistFileName = Path.Combine(
            FileSystemLayout.TranscodeFolder,
            request.ChannelNumber,
            "live.m3u8");

        int initialSegmentCount = await _configElementRepository
            .GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount)
            .Map(maybeCount => maybeCount.Match(identity, () => 1));

        await WaitForPlaylistSegments(playlistFileName, initialSegmentCount, worker, cancellationToken);

        return Unit.Default;
    }

    private async Task WaitForPlaylistSegments(
        string playlistFileName,
        int initialSegmentCount,
        HlsSessionWorker worker,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            DateTimeOffset start = DateTimeOffset.Now;
            DateTimeOffset finish = start.AddSeconds(8);

            _logger.LogDebug("Waiting for playlist to exist");
            while (!File.Exists(playlistFileName))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            _logger.LogDebug("Playlist exists");

            var segmentCount = 0;
            int lastSegmentCount = -1;
            while (DateTimeOffset.Now < finish && segmentCount < initialSegmentCount)
            {
                if (segmentCount != lastSegmentCount)
                {
                    lastSegmentCount = segmentCount;
                    _logger.LogDebug(
                        "Segment count {SegmentCount} of {InitialSegmentCount}",
                        segmentCount,
                        initialSegmentCount);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

                DateTimeOffset now = DateTimeOffset.Now.AddSeconds(-30);
                Option<TrimPlaylistResult> maybeResult = await worker.TrimPlaylist(now, cancellationToken);
                foreach (TrimPlaylistResult result in maybeResult)
                {
                    segmentCount = result.SegmentCount;
                }
            }
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug("WaitForPlaylistSegments took {Duration}", sw.Elapsed);
        }
    }

    private Task<Validation<BaseError, Unit>> Validate(StartFFmpegSession request) =>
        SessionMustBeInactive(request)
            .BindT(_ => FolderMustBeEmpty(request));

    private Task<Validation<BaseError, Unit>> SessionMustBeInactive(StartFFmpegSession request)
    {
        var result = Optional(_ffmpegSegmenterService.SessionWorkers.TryAdd(request.ChannelNumber, null))
            .Where(success => success)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>(new ChannelSessionAlreadyActive());

        if (result.IsFail && _ffmpegSegmenterService.SessionWorkers.TryGetValue(
                request.ChannelNumber,
                out IHlsSessionWorker worker))
        {
            worker?.Touch();
        }

        return result.AsTask();
    }

    private Task<Validation<BaseError, Unit>> FolderMustBeEmpty(StartFFmpegSession request)
    {
        string folder = Path.Combine(FileSystemLayout.TranscodeFolder, request.ChannelNumber);
        _logger.LogDebug("Preparing transcode folder {Folder}", folder);

        _localFileSystem.EnsureFolderExists(folder);
        _localFileSystem.EmptyFolder(folder);

        return Task.FromResult<Validation<BaseError, Unit>>(Unit.Default);
    }
}
