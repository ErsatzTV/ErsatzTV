using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming
{
    public class HlsSessionWorker : IHlsSessionWorker
    {
        private static int _workAheadCount;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<HlsSessionWorker> _logger;
        private DateTimeOffset _lastAccess;
        private DateTimeOffset _transcodedUntil;
        private Timer _timer;
        private readonly object _sync = new();
        private DateTimeOffset _playlistStart;

        public HlsSessionWorker(IServiceScopeFactory serviceScopeFactory, ILogger<HlsSessionWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public DateTimeOffset PlaylistStart => _playlistStart;

        public void Touch()
        {
            lock (_sync)
            {
                _lastAccess = DateTimeOffset.Now;

                _timer?.Stop();
                _timer?.Start();
            }
        }

        public async Task Run(string channelNumber, TimeSpan idleTimeout)
        {
            var cts = new CancellationTokenSource();
            void Cancel(object o, ElapsedEventArgs e) => cts.Cancel();

            try
            {
                lock (_sync)
                {
                    _timer = new Timer(idleTimeout.TotalMilliseconds) { AutoReset = false };
                    _timer.Elapsed += Cancel;
                }

                CancellationToken cancellationToken = cts.Token;

                _logger.LogInformation("Starting HLS session for channel {Channel}", channelNumber);

                Touch();
                _transcodedUntil = DateTimeOffset.Now;
                _playlistStart = _transcodedUntil;

                bool initialWorkAhead = Volatile.Read(ref _workAheadCount) < await GetWorkAheadLimit();
                if (!await Transcode(channelNumber, true, !initialWorkAhead, cancellationToken))
                {
                    return;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (DateTimeOffset.Now - _lastAccess > idleTimeout)
                    {
                        _logger.LogInformation("Stopping idle HLS session for channel {Channel}", channelNumber);
                        return;
                    }

                    var transcodedBuffer = TimeSpan.FromSeconds(
                        Math.Max(0, _transcodedUntil.Subtract(DateTimeOffset.Now).TotalSeconds));
                    if (transcodedBuffer <= TimeSpan.FromMinutes(1))
                    {
                        // only use realtime encoding when we're at least 30 seconds ahead
                        bool realtime = transcodedBuffer >= TimeSpan.FromSeconds(30);
                        bool subsequentWorkAhead =
                            !realtime && Volatile.Read(ref _workAheadCount) < await GetWorkAheadLimit();
                        if (!await Transcode(channelNumber, false, !subsequentWorkAhead, cancellationToken))
                        {
                            return;
                        }
                    }
                    else
                    {
                        await TrimAndDelete(channelNumber, cancellationToken);
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            }
            finally
            {
                lock (_sync)
                {
                    _timer.Elapsed -= Cancel;
                }
            }
        }

        private async Task<bool> Transcode(string channelNumber, bool firstProcess, bool realtime, CancellationToken cancellationToken)
        {
            try
            {
                if (!realtime)
                {
                    Interlocked.Increment(ref _workAheadCount);
                    _logger.LogInformation("HLS segmenter will work ahead for channel {Channel}", channelNumber);
                }
                else
                {
                    _logger.LogInformation(
                        "HLS segmenter will NOT work ahead for channel {Channel}",
                        channelNumber);
                }

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var request = new GetPlayoutItemProcessByChannelNumber(
                    channelNumber,
                    "segmenter",
                    firstProcess ? DateTimeOffset.Now : _transcodedUntil.AddSeconds(1),
                    !firstProcess,
                    realtime);

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
                    await TrimAndDelete(channelNumber, cancellationToken);

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
            finally
            {
                Interlocked.Decrement(ref _workAheadCount);
            }

            return true;
        }
        
        private async Task TrimAndDelete(string channelNumber, CancellationToken cancellationToken)
        {
            string playlistFileName = Path.Combine(
                FileSystemLayout.TranscodeFolder,
                channelNumber,
                "live.m3u8");

            if (File.Exists(playlistFileName))
            {
                // trim playlist and insert discontinuity before appending with new ffmpeg process
                string[] lines = await File.ReadAllLinesAsync(playlistFileName, cancellationToken);
                TrimPlaylistResult trimResult = HlsPlaylistFilter.TrimPlaylistWithDiscontinuity(
                    _playlistStart,
                    DateTimeOffset.Now.AddMinutes(-1),
                    lines);
                await File.WriteAllTextAsync(playlistFileName, trimResult.Playlist, cancellationToken);

                // delete old segments
                foreach (string file in Directory.GetFiles(
                    Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber),
                    "*.ts"))
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.StartsWith("live") && int.Parse(fileName.Replace("live", string.Empty).Split('.')[0]) <
                        trimResult.Sequence)
                    {
                        File.Delete(file);
                    }
                }

                _playlistStart = trimResult.PlaylistStart;
            }
        }

        private async Task<int> GetWorkAheadLimit()
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IConfigElementRepository repo = scope.ServiceProvider.GetRequiredService<IConfigElementRepository>();
            return await repo.GetValue<int>(ConfigElementKey.FFmpegWorkAheadSegmenters)
                .Map(maybeCount => maybeCount.Match(identity, () => 1));
        }
    }
}
