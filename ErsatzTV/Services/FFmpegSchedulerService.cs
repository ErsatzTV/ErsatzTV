using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.Streaming.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services
{
    public class FFmpegSchedulerService : BackgroundService
    {
        private readonly ILogger<FFmpegSchedulerService> _logger;
        private readonly ChannelWriter<IFFmpegWorkerRequest> _workerChannel;

        public FFmpegSchedulerService(
            ChannelWriter<IFFmpegWorkerRequest> workerChannel,
            ILogger<FFmpegSchedulerService> logger)
        {
            _workerChannel = workerChannel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _workerChannel.WriteAsync(new CleanUpFFmpegSessions(), cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}
