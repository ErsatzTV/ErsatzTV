using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.Streaming.Commands;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Services
{
    public class FFmpegWorkerService : BackgroundService
    {
        private readonly ChannelReader<IFFmpegWorkerRequest> _channel;
        private readonly ChannelWriter<IFFmpegWorkerRequest> _channelWriter;
        private readonly ILogger<FFmpegWorkerService> _logger;
        private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FFmpegWorkerService(
            ChannelReader<IFFmpegWorkerRequest> channel,
            ChannelWriter<IFFmpegWorkerRequest> channelWriter,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<FFmpegWorkerService> logger,
            IFFmpegSegmenterService ffmpegSegmenterService)
        {
            _channel = channel;
            _channelWriter = channelWriter;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _ffmpegSegmenterService = ffmpegSegmenterService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FFmpeg worker service started");

            await foreach (IFFmpegWorkerRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    switch (request)
                    {
                        case TouchFFmpegSession touchFFmpegSession:
                            foreach (DirectoryInfo parent in Optional(Directory.GetParent(touchFFmpegSession.Path)))
                            {
                                _ffmpegSegmenterService.TouchChannel(parent.Name);
                            }
                            break;
                        case CleanUpFFmpegSessions:
                            _ffmpegSegmenterService.CleanUpSessions();
                            break;
                        case StartFFmpegSession startFFmpegSession:
                            _logger.LogInformation(
                                "Starting ffmpeg session for channel {Channel}",
                                startFFmpegSession.ChannelNumber);

                            if (!_ffmpegSegmenterService.ProcessExistsForChannel(startFFmpegSession.ChannelNumber))
                            {
                                var req = new GetPlayoutItemProcessByChannelNumber(
                                    startFFmpegSession.ChannelNumber,
                                    "segmenter",
                                    startFFmpegSession.StartAtZero);
                                Either<BaseError, Process> maybeProcess = await mediator.Send(req, cancellationToken);
                                maybeProcess.Match(
                                    process =>
                                    {
                                        if (_ffmpegSegmenterService.TryAdd(startFFmpegSession.ChannelNumber, process))
                                        {
                                            _logger.LogDebug(
                                                "ffmpeg hls arguments {FFmpegArguments}",
                                                string.Join(" ", process.StartInfo.ArgumentList));

                                            process.Start();
                                            process.EnableRaisingEvents = true;
                                            process.Exited += (_, _) =>
                                            {
                                                if (process.ExitCode == 0)
                                                {
                                                    _channelWriter.TryWrite(
                                                        new StartFFmpegSession(startFFmpegSession.ChannelNumber, true));
                                                }
                                                else
                                                {
                                                    _logger.LogDebug(
                                                        "hls segmenter for channel {Channel} exited with code {ExitCode}",
                                                        startFFmpegSession.ChannelNumber,
                                                        process.ExitCode);
                                                }
                                            };
                                        }
                                    },
                                    _ => { });
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                     _logger.LogWarning(ex, "Failed to handle ffmpeg worker request");
                }
            }
            
            // kill any running processes after cancellation
            _ffmpegSegmenterService.KillAll();
        }
    }
}
