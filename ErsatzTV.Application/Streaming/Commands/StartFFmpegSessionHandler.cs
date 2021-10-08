using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Commands
{
    public class StartFFmpegSessionHandler : MediatR.IRequestHandler<StartFFmpegSession, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IFFmpegWorkerRequest> _channel;
        private readonly ILogger<StartFFmpegSessionHandler> _logger;
        private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
        private readonly ILocalFileSystem _localFileSystem;

        public StartFFmpegSessionHandler(
            IFFmpegSegmenterService ffmpegSegmenterService,
            ILocalFileSystem localFileSystem,
            ChannelWriter<IFFmpegWorkerRequest> channel,
            ILogger<StartFFmpegSessionHandler> logger)
        {
            _ffmpegSegmenterService = ffmpegSegmenterService;
            _localFileSystem = localFileSystem;
            _channel = channel;
            _logger = logger;
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
            await _channel.WriteAsync(request);

            // TODO: find some other way to let ffmpeg get ahead
            await Task.Delay(TimeSpan.FromSeconds(5));

            return Unit.Default;
        }

        private Task<Validation<BaseError, Unit>> Validate(StartFFmpegSession request) =>
            ProcessMustNotExist(request)
                .BindT(_ => FolderMustBeEmpty(request));

        private Task<Validation<BaseError, Unit>> ProcessMustNotExist(StartFFmpegSession request) =>
            Optional(_ffmpegSegmenterService.ProcessExistsForChannel(request.ChannelNumber))
                .Filter(exists => exists == false)
                .Map(_ => Unit.Default)
                .ToValidation<BaseError>(new ChannelHasProcess())
                .AsTask();

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
