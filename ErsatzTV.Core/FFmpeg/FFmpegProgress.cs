using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public partial class FFmpegProgress
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

    public void LogSpeed(Option<int> mediaItemId, bool isWorkingAhead, string channelNumber, ILogger logger)
    {
        foreach (double speed in Speed)
        {
            if (isWorkingAhead)
            {
                if (speed < 1.0)
                {
                    logger.LogCritical(
                        "Media item {MediaItemId} on channel {Channel} transcoded at {Speed}x (NOT throttled) which is NOT fast enough to support playback",
                        mediaItemId,
                        channelNumber,
                        speed);
                }
                else if (speed <= 1.5)
                {
                    logger.LogWarning(
                        "Media item {MediaItemId} on channel {Channel} transcoded at {Speed}x (NOT throttled) which may not be fast enough to support playback",
                        mediaItemId,
                        channelNumber,
                        speed);
                }
            }
            else if (speed < 1.0)
            {
                logger.LogWarning(
                    "Media item {MediaItemId} on channel {Channel} transcoded at {Speed}x (throttled) which may not be fast enough to support playback",
                    mediaItemId,
                    channelNumber,
                    speed);
            }
        }
    }

    [GeneratedRegex(@"speed=\s*([\d\.]+)x", RegexOptions.IgnoreCase)]
    private static partial Regex FFmpegSpeed();
}
