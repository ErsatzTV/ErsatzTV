using System.Diagnostics;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegProcess : Process
{
    public static int ProcessCount;

    public FFmpegProcess() => Interlocked.Increment(ref ProcessCount);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Interlocked.Decrement(ref ProcessCount);
    }
}
