using System.Diagnostics;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegProcess : Process
{
    private static int _processCount;

    public static int ProcessCount => _processCount;

    public FFmpegProcess() => Interlocked.Increment(ref _processCount);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Interlocked.Decrement(ref _processCount);
    }
}
