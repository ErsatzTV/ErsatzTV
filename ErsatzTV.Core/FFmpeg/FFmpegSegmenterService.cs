using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegSegmenterService : IFFmpegSegmenterService
{
    private readonly ILogger<FFmpegSegmenterService> _logger;

    public FFmpegSegmenterService(ILogger<FFmpegSegmenterService> logger)
    {
        _logger = logger;

        SessionWorkers = new ConcurrentDictionary<string, IHlsSessionWorker>();
    }

    public ConcurrentDictionary<string, IHlsSessionWorker> SessionWorkers { get; }

    public void TouchChannel(string channelNumber)
    {
        if (SessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            worker?.Touch();
        }
    }

    public void PlayoutUpdated(string channelNumber)
    {
        if (SessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            if (worker != null)
            {
                _logger.LogInformation(
                    "Playout has been updated for channel {ChannelNumber}, HLS segmenter will skip ahead to catch up",
                    channelNumber);

                worker.PlayoutUpdated();
            }
        }
    }
}
