using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.Maintenance.Commands;
using ErsatzTV.Application.MediaSources.Commands;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Application.Search.Commands;
using ErsatzTV.Application.Streaming.Commands;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Services
{
    public class FFmpegWorkerService : BackgroundService
    {
        private readonly ChannelReader<IFFmpegWorkerRequest> _channel;
        private readonly ILogger<FFmpegWorkerService> _logger;
        private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FFmpegWorkerService(
            ChannelReader<IFFmpegWorkerRequest> channel,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<FFmpegWorkerService> logger,
            IFFmpegSegmenterService ffmpegSegmenterService)
        {
            _channel = channel;
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

                    // TODO: wait for signal to stop process
                    switch (request)
                    {
                        // wait for signal to start process
                        case StartFFmpegSession startFFmpegSession:
                            // TODO: see if this gets called with each playlist refresh
                            // TODO: don't use realtime (maybe fast for x seconds, then realtime - in the future)
                            _logger.LogInformation("Starting ffmpeg session");

                            if (!_ffmpegSegmenterService.ProcessExistsForChannel(startFFmpegSession.ChannelNumber))
                            {
                                var req = new GetPlayoutItemProcessByChannelNumber(startFFmpegSession.ChannelNumber, "segmenter");
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
