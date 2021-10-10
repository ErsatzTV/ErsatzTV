using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegSegmenterService : IFFmpegSegmenterService
    {
        public FFmpegSegmenterService()
        {
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
    }
}
