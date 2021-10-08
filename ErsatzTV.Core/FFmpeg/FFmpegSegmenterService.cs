using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegSegmenterService : IFFmpegSegmenterService
    {
        private static readonly ConcurrentDictionary<string, ProcessAndToken> Processes = new();

        private readonly ILogger<FFmpegSegmenterService> _logger;

        public FFmpegSegmenterService(ILogger<FFmpegSegmenterService> logger) => _logger = logger;

        public bool ProcessExistsForChannel(string channelNumber)
        {
            if (Processes.TryGetValue(channelNumber, out ProcessAndToken processAndToken))
            {
                if (!processAndToken.Process.HasExited || !Processes.TryRemove(
                    new KeyValuePair<string, ProcessAndToken>(channelNumber, processAndToken)))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAdd(string channelNumber, Process process)
        {
            var cts = new CancellationTokenSource();
            var processAndToken = new ProcessAndToken(process, cts, DateTimeOffset.Now);
            if (Processes.TryAdd(channelNumber, processAndToken))
            {
                CancellationToken token = cts.Token;
                token.Register(process.Kill);
                return true;
            }

            return false;
        }

        public void TouchChannel(string channelNumber)
        {
            if (Processes.TryGetValue(channelNumber, out ProcessAndToken processAndToken))
            {
                ProcessAndToken newValue = processAndToken with { LastAccess = DateTimeOffset.Now };
                if (!Processes.TryUpdate(channelNumber, newValue, processAndToken))
                {
                    _logger.LogWarning("Failed to update last access for channel {Channel}", channelNumber);
                }
            }
        }

        public void CleanUpSessions()
        {
            foreach ((string key, (_, CancellationTokenSource cts, DateTimeOffset lastAccess)) in Processes.ToList())
            {
                // TODO: configure this time span? 5 min?
                if (DateTimeOffset.Now.Subtract(lastAccess) > TimeSpan.FromMinutes(2))
                {
                    _logger.LogDebug("Cleaning up ffmpeg session for channel {Channel}", key);

                    cts.Cancel();
                    Processes.TryRemove(key, out _);
                }
            }
        }

        public Unit KillAll()
        {
            foreach ((string key, ProcessAndToken processAndToken) in Processes.ToList())
            {
                try
                {
                    processAndToken.TokenSource.Cancel();
                    Processes.TryRemove(key, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Error killing process");
                }
            }

            return Unit.Default;
        }

        private record ProcessAndToken(Process Process, CancellationTokenSource TokenSource, DateTimeOffset LastAccess);
    }
}
