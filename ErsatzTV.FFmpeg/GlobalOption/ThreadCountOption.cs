using System.Globalization;

namespace ErsatzTV.FFmpeg.GlobalOption;

public class ThreadCountOption : GlobalOption
{
    private readonly int _threadCount;

    public ThreadCountOption(int threadCount) => _threadCount = threadCount;

    public override IList<string> GlobalOptions => new List<string>
        { "-threads", _threadCount.ToString(CultureInfo.InvariantCulture) };
}
