using System.Globalization;

namespace ErsatzTV.Application.Streaming;

public record PtsTime(long Value)
{
    public static readonly PtsTime Zero = new(0);

    public static PtsTime From(string ffprobeLine)
    {
        string[] split = ffprobeLine.Split("|");
        var ptsTime = long.Parse(split[0], CultureInfo.InvariantCulture);
        if (long.TryParse(split[1], CultureInfo.InvariantCulture, out long duration))
        {
            ptsTime += duration;
        }
        return new PtsTime(ptsTime);
    }
}
