﻿using System.Globalization;

namespace ErsatzTV.Application.Streaming;

public record PtsTime(double Value)
{
    public static readonly PtsTime Zero = new(0);

    public static PtsTime From(string ffprobeLine)
    {
        string[] split = ffprobeLine.Split("|");
        var ptsTime = double.Parse(split[0], CultureInfo.InvariantCulture);
        if (double.TryParse(split[1], CultureInfo.InvariantCulture, out double duration))
        {
            ptsTime += duration;
        }
        return new PtsTime(ptsTime);
    }
}
