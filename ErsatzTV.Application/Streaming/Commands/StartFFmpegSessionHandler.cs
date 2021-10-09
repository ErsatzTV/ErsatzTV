using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Commands
{
    public class StartFFmpegSessionHandler : MediatR.IRequestHandler<StartFFmpegSession, Either<BaseError, Unit>>
    {
        private readonly ILogger<StartFFmpegSessionHandler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
        private readonly ILocalFileSystem _localFileSystem;

        public StartFFmpegSessionHandler(
            ILocalFileSystem localFileSystem,
            ILogger<StartFFmpegSessionHandler> logger,
            IServiceScopeFactory serviceScopeFactory,
            IFFmpegSegmenterService ffmpegSegmenterService)
        {
            _localFileSystem = localFileSystem;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _ffmpegSegmenterService = ffmpegSegmenterService;
        }

        public Task<Either<BaseError, Unit>> Handle(StartFFmpegSession request, CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => StartProcess(request))
                // this weirdness is needed to maintain the error type (.ToEitherAsync() just gives BaseError)
#pragma warning disable VSTHRD103
                .Bind(v => v.ToEither().MapLeft(seq => seq.Head()).MapAsync<BaseError, Task<Unit>, Unit>(identity));
#pragma warning restore VSTHRD103

        private async Task<Unit> StartProcess(StartFFmpegSession request)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            HlsSessionWorker worker = scope.ServiceProvider.GetRequiredService<HlsSessionWorker>();
            _ffmpegSegmenterService.SessionWorkers.AddOrUpdate(request.ChannelNumber, _ => worker, (_, _) => worker);

            // fire and forget worker
            _ = worker.Run(request.ChannelNumber)
                .ContinueWith(
                    _ => _ffmpegSegmenterService.SessionWorkers.TryRemove(
                        request.ChannelNumber,
                        out IHlsSessionWorker _),
                    TaskScheduler.Default);

            string playlistFileName = Path.Combine(
                FileSystemLayout.TranscodeFolder,
                request.ChannelNumber,
                "live.m3u8");

            while (!File.Exists(playlistFileName))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            return Unit.Default;
        }

        private Task<Validation<BaseError, Unit>> Validate(StartFFmpegSession request) =>
            SessionMustBeInactive(request)
                .BindT(_ => FolderMustBeEmpty(request));

        private Task<Validation<BaseError, Unit>> SessionMustBeInactive(StartFFmpegSession request)
        {
            var result = Optional(_ffmpegSegmenterService.SessionWorkers.TryAdd(request.ChannelNumber, null))
                .Filter(success => success)
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
}
