using System.Text.RegularExpressions;

namespace ErsatzTV.Core.FFmpeg;

public partial class FFmpegProgressParser
{
    public Option<double> Speed { get; private set; } = Option<double>.None;

    public void ParseLine(string line)
    {
        Match match = FFmpegSpeed().Match(line);
        if (match.Success && double.TryParse(match.Groups[1].Value, out double speed))
        {
            Speed = speed;
        }
    }

    [GeneratedRegex(@"speed=\s*([\d\.]+)x", RegexOptions.IgnoreCase)]
    private static partial Regex FFmpegSpeed();
}
