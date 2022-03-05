using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Streaming;

public class StartFFmpegSessionHandler : MediatR.IRequestHandler<StartFFmpegSession, Either<BaseError, Unit>>
{
    private readonly ILogger<StartFFmpegSessionHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IHlsPlaylistFilter _hlsPlaylistFilter;
    private readonly ILocalFileSystem _localFileSystem;

    public StartFFmpegSessionHandler(
        ILocalFileSystem localFileSystem,
        ILogger<StartFFmpegSessionHandler> logger,
        IServiceScopeFactory serviceScopeFactory,
        IFFmpegSegmenterService ffmpegSegmenterService,
        IConfigElementRepository configElementRepository,
        IHlsPlaylistFilter hlsPlaylistFilter)
    {
        _localFileSystem = localFileSystem;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _configElementRepository = configElementRepository;
        _hlsPlaylistFilter = hlsPlaylistFilter;
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

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        HlsSessionWorker worker = scope.ServiceProvider.GetRequiredService<HlsSessionWorker>();
        _ffmpegSegmenterService.SessionWorkers.AddOrUpdate(request.ChannelNumber, _ => worker, (_, _) => worker);

        // fire and forget worker
        _ = worker.Run(request.ChannelNumber, idleTimeout)
            .ContinueWith(
                _ => _ffmpegSegmenterService.SessionWorkers.TryRemove(
                    request.ChannelNumber,
                    out IHlsSessionWorker _),
                TaskScheduler.Default);

        string playlistFileName = Path.Combine(
            FileSystemLayout.TranscodeFolder,
            request.ChannelNumber,
            "live.m3u8");

        IConfigElementRepository repo = scope.ServiceProvider.GetRequiredService<IConfigElementRepository>();
        int initialSegmentCount = await repo.GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount)
            .Map(maybeCount => maybeCount.Match(identity, () => 1));

        await WaitForPlaylistSegments(playlistFileName, initialSegmentCount, worker, cancellationToken);

        return Unit.Default;
    }

    private async Task WaitForPlaylistSegments(
        string playlistFileName,
        int initialSegmentCount,
        IHlsSessionWorker worker,
        CancellationToken cancellationToken)
    {
        while (!File.Exists(playlistFileName))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        var segmentCount = 0;
        while (segmentCount < initialSegmentCount)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

            DateTimeOffset now = DateTimeOffset.Now.AddSeconds(-30);
            Option<TrimPlaylistResult> maybeResult = await worker.TrimPlaylist(now, cancellationToken);
            foreach (TrimPlaylistResult result in maybeResult)
            {
                segmentCount = result.SegmentCount;
            }
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