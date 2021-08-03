using System.Diagnostics;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.FFmpeg
{
    public interface IFFmpegSegmenterService
    {
        bool ProcessExistsForChannel(string channelNumber);
        bool TryAdd(string channelNumber, Process process);
        Unit KillAll();
    }
}
