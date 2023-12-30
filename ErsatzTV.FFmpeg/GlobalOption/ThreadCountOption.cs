using System.Globalization;

namespace ErsatzTV.FFmpeg.GlobalOption;

public class ThreadCountOption : GlobalOption
{
    private readonly int _threadCount;

    public ThreadCountOption(int threadCount) => _threadCount = threadCount;

    public override string[] GlobalOptions => new[]
        { "-threads", _threadCount.ToString(CultureInfo.InvariantCulture) };
}
