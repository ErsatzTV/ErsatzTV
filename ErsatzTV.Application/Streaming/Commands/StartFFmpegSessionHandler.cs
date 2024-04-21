﻿using System.Threading.Channels;
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
    private readonly ILogger<HlsSessionWorkerV2> _sessionWorkerV2Logger;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public StartFFmpegSessionHandler(
        IHlsPlaylistFilter hlsPlaylistFilter,
        IServiceScopeFactory serviceScopeFactory,
        IMediator mediator,
        IClient client,
        ILocalFileSystem localFileSystem,
        ILogger<StartFFmpegSessionHandler> logger,
        ILogger<HlsSessionWorker> sessionWorkerLogger,
        ILogger<HlsSessionWorkerV2> sessionWorkerV2Logger,
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
        _sessionWorkerV2Logger = sessionWorkerV2Logger;
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

        IHlsSessionWorker worker = GetSessionWorker(request, targetFramerate);

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
            .GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount)
            .Map(maybeCount => maybeCount.Match(identity, () => 1));

        await worker.WaitForPlaylistSegments(initialSegmentCount, cancellationToken);

        return Unit.Default;
    }

    private IHlsSessionWorker GetSessionWorker(StartFFmpegSession request, Option<int> targetFramerate) =>
        request.Mode switch
        {
            "segmenter-v2" => new HlsSessionWorkerV2(
                _serviceScopeFactory,
                _localFileSystem,
                _sessionWorkerV2Logger,
                targetFramerate,
                request.Scheme,
                request.Host),
            _ => new HlsSessionWorker(
                _serviceScopeFactory,
                _client,
                _hlsPlaylistFilter,
                _configElementRepository,
                _localFileSystem,
                _sessionWorkerLogger,
                targetFramerate)
        };

    private Task<Validation<BaseError, Unit>> Validate(StartFFmpegSession request) =>
        SessionMustBeInactive(request)
            .BindT(_ => FolderMustBeEmpty(request));

    private Task<Validation<BaseError, Unit>> SessionMustBeInactive(StartFFmpegSession request)
    {
        var result = Optional(_ffmpegSegmenterService.TryAddWorker(request.ChannelNumber, null))
            .Where(success => success)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>(new ChannelSessionAlreadyActive());

        if (result.IsFail && _ffmpegSegmenterService.TryGetWorker(
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
