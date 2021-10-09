using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace ErsatzTV.Application.Streaming
{
    public class HlsSessionWorker : IHlsSessionWorker
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<HlsSessionWorker> _logger;
        private DateTimeOffset _lastAccess;
        private DateTimeOffset _transcodedUntil;
        private readonly Timer _timer = new(TimeSpan.FromMinutes(2).TotalMilliseconds) { AutoReset = false };
        private readonly object _sync = new();

        public HlsSessionWorker(IServiceScopeFactory serviceScopeFactory, ILogger<HlsSessionWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public void Touch()
        {
            lock (_sync)
            {
                _lastAccess = DateTimeOffset.Now;

                _timer.Stop();
                _timer.Start();
            }
        }

        public async Task Run(string channelNumber)
        {
            var cts = new CancellationTokenSource();
            void Cancel(object o, ElapsedEventArgs e) => cts.Cancel();

            try
            {
                _timer.Elapsed += Cancel;

                CancellationToken cancellationToken = cts.Token;

                _logger.LogInformation("Starting HLS session for channel {Channel}", channelNumber);

                Touch();
                _transcodedUntil = DateTimeOffset.Now;

                // start initial transcode WITHOUT realtime throttle
                if (!await Transcode(channelNumber, true, cancellationToken))
                {
                    return;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    // TODO: configurable? 5 minutes?
                    if (DateTimeOffset.Now - _lastAccess > TimeSpan.FromMinutes(2))
                    {
                        _logger.LogInformation("Stopping idle HLS session for channel {Channel}", channelNumber);
                        return;
                    }

                    if (DateTimeOffset.Now + TimeSpan.FromMinutes(1) > _transcodedUntil)
                    {
                        if (!await Transcode(channelNumber, false, cancellationToken))
                        {
                            return;
                        }
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            }
            finally
            {
                _timer.Elapsed -= Cancel;
            }
        }

        private async Task<bool> Transcode(string channelNumber, bool firstProcess, CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var request = new GetPlayoutItemProcessByChannelNumber(
                    channelNumber,
                    "segmenter",
                    firstProcess ? DateTimeOffset.Now : _transcodedUntil.AddSeconds(1),
                    !firstProcess);

                // _logger.LogInformation("Request {@Request}", request);

                Either<BaseError, PlayoutItemProcessModel> result = await mediator.Send(request, cancellationToken);

                // _logger.LogInformation("Result {Result}", result.ToString());

                foreach (BaseError error in result.LeftAsEnumerable())
                {
                    _logger.LogWarning(
                        "Failed to create process for HLS session on channel {Channel}: {Error}",
                        channelNumber,
                        error.ToString());

                    return false;
                }

                foreach (PlayoutItemProcessModel processModel in result.RightAsEnumerable())
                {
                    Process process = processModel.Process;

                    _logger.LogDebug(
                        "ffmpeg hls arguments {FFmpegArguments}",
                        string.Join(" ", process.StartInfo.ArgumentList));

                    process.Start();
                    try
                    {
                        await process.WaitForExitAsync(cancellationToken);
                        process.WaitForExit();
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogInformation("Terminating HLS process for channel {Channel}", channelNumber);
                        process.Kill();
                        process.WaitForExit();

                        return false;
                    }

                    _logger.LogInformation("HLS process has completed for channel {Channel}", channelNumber);

                    _transcodedUntil = processModel.Until;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transcoding channel {Channel}", channelNumber);
                return false;
            }

            return true;
        }
    }
}
