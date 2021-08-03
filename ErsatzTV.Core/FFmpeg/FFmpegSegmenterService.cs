using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegSegmenterService : IFFmpegSegmenterService
    {
        private static readonly ConcurrentDictionary<string, Process> Processes = new();

        private readonly ILogger<FFmpegSegmenterService> _logger;

        public FFmpegSegmenterService(ILogger<FFmpegSegmenterService> logger) => _logger = logger;

        public bool ProcessExistsForChannel(string channelNumber) =>
            Processes.ContainsKey(channelNumber);

        public bool TryAdd(string channelNumber, Process process) =>
            Processes.TryAdd(channelNumber, process);

        public Unit KillAll()
        {
            foreach ((string _, Process process) in Processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Error killing process");
                }
            }

            return Unit.Default;
        }
    }
}
