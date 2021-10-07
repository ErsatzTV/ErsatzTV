using System.Diagnostics;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.FFmpeg
{
    public interface IFFmpegSegmenterService
    {
        bool ProcessExistsForChannel(string channelNumber);
        bool TryAdd(string channelNumber, Process process);
        void TouchChannel(string channelNumber);
        void CleanUpSessions();
        Unit KillAll();
    }
}
