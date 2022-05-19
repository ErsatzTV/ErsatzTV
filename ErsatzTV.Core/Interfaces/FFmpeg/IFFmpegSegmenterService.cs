using System.Collections.Concurrent;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegSegmenterService
{
    ConcurrentDictionary<string, IHlsSessionWorker> SessionWorkers { get; }

    void TouchChannel(string channelNumber);
    void PlayoutUpdated(string channelNumber);
}
