using System.Globalization;
using System.IO.Abstractions;
using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.OutputFormat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Streaming;

public class StartFFmpegSessionHandler : IRequestHandler<StartFFmpegSession, Either<BaseError, string>>
{
    private readonly IFileSystem _fileSystem;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly IGraphicsEngine _graphicsEngine;
    private readonly IHlsPlaylistFilter _hlsPlaylistFilter;
    private readonly IHlsInitSegmentCache _hlsInitSegmentCache;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<StartFFmpegSessionHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<HlsSessionWorker> _sessionWorkerLogger;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public StartFFmpegSessionHandler(
        IHlsPlaylistFilter hlsPlaylistFilter,
        IHlsInitSegmentCache hlsInitSegmentCache,
        IServiceScopeFactory serviceScopeFactory,
        IMediator mediator,
        IFileSystem fileSystem,
        ILocalFileSystem localFileSystem,
        ILogger<StartFFmpegSessionHandler> logger,
        ILogger<HlsSessionWorker> sessionWorkerLogger,
        IFFmpegSegmenterService ffmpegSegmenterService,
        IConfigElementRepository configElementRepository,
        IGraphicsEngine graphicsEngine,
        IHostApplicationLifetime hostApplicationLifetime,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _hlsPlaylistFilter = hlsPlaylistFilter;
        _hlsInitSegmentCache = hlsInitSegmentCache;
        _serviceScopeFactory = serviceScopeFactory;
        _mediator = mediator;
        _fileSystem = fileSystem;
        _localFileSystem = localFileSystem;
        _logger = logger;
        _sessionWorkerLogger = sessionWorkerLogger;
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _configElementRepository = configElementRepository;
        _graphicsEngine = graphicsEngine;
        _hostApplicationLifetime = hostApplicationLifetime;
        _workerChannel = workerChannel;
    }

    public Task<Either<BaseError, string>> Handle(StartFFmpegSession request, CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(_ => StartProcess(request, cancellationToken))
            // this weirdness is needed to maintain the error type (.ToEitherAsync() just gives BaseError)
#pragma warning disable VSTHRD103
            .Bind(v => v.ToEither().MapLeft(seq => seq.Head()).MapAsync<BaseError, Task<string>, string>(identity));
#pragma warning restore VSTHRD103

    private async Task<string> StartProcess(StartFFmpegSession request, CancellationToken cancellationToken)
    {
        Option<TimeSpan> idleTimeout = await _configElementRepository
            .GetValue<int>(ConfigElementKey.FFmpegSegmenterTimeout, cancellationToken)
            .Map(maybeTimeout => maybeTimeout.Match(i => TimeSpan.FromSeconds(i), () => TimeSpan.FromMinutes(1)));

        Option<FrameRate> targetFramerate = await _mediator.Send(
            new GetChannelFramerate(request.ChannelNumber),
            cancellationToken);

        // disable idle timeout when configured to keep running
        Option<ChannelViewModel> channel =
            await _mediator.Send(new GetChannelByNumber(request.ChannelNumber), cancellationToken);
        if (await channel.Map(c => c.IdleBehavior is ChannelIdleBehavior.KeepRunning).IfNoneAsync(false))
        {
            idleTimeout = Option<TimeSpan>.None;
        }

        await _mediator.Send(new RefreshGraphicsElements(), cancellationToken);

        HlsSessionWorker worker = GetSessionWorker(request, targetFramerate);

        _ffmpegSegmenterService.AddOrUpdateWorker(request.ChannelNumber, worker);

        // fire and forget worker
        _ = worker.Run(request.ChannelNumber, idleTimeout, _hostApplicationLifetime.ApplicationStopping)
            .ContinueWith(
                _ =>
                {
                    _ffmpegSegmenterService.RemoveWorker(request.ChannelNumber, out IHlsSessionWorker inactiveWorker);

                    inactiveWorker?.Dispose();

                    _workerChannel.TryWrite(new ReleaseMemory(false));
                },
                TaskScheduler.Default);

        int initialSegmentCount = await _configElementRepository
            .GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount, cancellationToken)
            .Map(maybeCount => maybeCount.Match(identity, () => 1));

        await worker.WaitForPlaylistSegments(initialSegmentCount, cancellationToken);

        return await GetMultiVariantPlaylist(request);
    }

    private HlsSessionWorker GetSessionWorker(StartFFmpegSession request, Option<FrameRate> targetFramerate) =>
        request.Mode switch
        {
            _ => new HlsSessionWorker(
                _serviceScopeFactory,
                _graphicsEngine,
                OutputFormatKind.Hls,
                _hlsPlaylistFilter,
                _hlsInitSegmentCache,
                _configElementRepository,
                _fileSystem,
                _localFileSystem,
                _sessionWorkerLogger,
                targetFramerate)
        };

    private Task<Validation<BaseError, Unit>> Validate(StartFFmpegSession request) =>
        SessionMustBeInactive(request)
            .BindT(_ => FolderMustBeEmpty(request));

    private async Task<Validation<BaseError, Unit>> SessionMustBeInactive(StartFFmpegSession request)
    {
        var result = Optional(_ffmpegSegmenterService.TryAddWorker(request.ChannelNumber, null))
            .Where(success => success)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>(new ChannelSessionAlreadyActive(await GetMultiVariantPlaylist(request)));

        if (result.IsFail && _ffmpegSegmenterService.TryGetWorker(
                request.ChannelNumber,
                out IHlsSessionWorker worker))
        {
            worker?.Touch(Option<string>.None);
        }

        return result;
    }

    private Task<Validation<BaseError, Unit>> FolderMustBeEmpty(StartFFmpegSession request)
    {
        string folder = Path.Combine(FileSystemLayout.TranscodeFolder, request.ChannelNumber);
        _logger.LogDebug("Preparing transcode folder {Folder}", folder);

        _localFileSystem.EnsureFolderExists(folder);
        _localFileSystem.EmptyFolder(folder);

        return Task.FromResult<Validation<BaseError, Unit>>(Unit.Default);
    }

    private async Task<string> GetMultiVariantPlaylist(StartFFmpegSession request)
    {
        var variantPlaylist =
            $"{request.Scheme}://{request.Host}{request.PathBase}/iptv/session/{request.ChannelNumber}/hls.m3u8{request.AccessTokenQuery}";

        Option<ChannelStreamingSpecsViewModel> maybeStreamingSpecs =
            await _mediator.Send(new GetChannelStreamingSpecs(request.ChannelNumber));
        string resolution = string.Empty;
        var bitrate = "10000000";
        foreach (ChannelStreamingSpecsViewModel streamingSpecs in maybeStreamingSpecs)
        {
            string videoCodec = streamingSpecs.VideoFormat switch
            {
                FFmpegProfileVideoFormat.Av1 => "av01.0.01M.08",
                FFmpegProfileVideoFormat.Hevc => "hvc1.1.6.L93.B0",
                FFmpegProfileVideoFormat.H264 => "avc1.4D4028",
                _ => string.Empty
            };

            string audioCodec = streamingSpecs.AudioFormat switch
            {
                FFmpegProfileAudioFormat.Ac3 => "ac-3",
                FFmpegProfileAudioFormat.Aac or FFmpegProfileAudioFormat.AacLatm => "mp4a.40.2",
                _ => string.Empty
            };

            List<string> codecStrings = [];
            if (!string.IsNullOrWhiteSpace(videoCodec))
            {
                codecStrings.Add(videoCodec);
            }

            if (!string.IsNullOrWhiteSpace(audioCodec))
            {
                codecStrings.Add(audioCodec);
            }

            string codecs = codecStrings.Count > 0 ? $",CODECS=\"{string.Join(",", codecStrings)}\"" : string.Empty;
            resolution = $",RESOLUTION={streamingSpecs.Width}x{streamingSpecs.Height}{codecs}";
            bitrate = streamingSpecs.Bitrate.ToString(CultureInfo.InvariantCulture);
        }

        return $@"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-STREAM-INF:BANDWIDTH={bitrate}{resolution}
{variantPlaylist}";
    }
}
