using System;
using System.Globalization;

namespace ErsatzTV.Core.FFmpeg;

public abstract record FadePoint(TimeSpan Time, string InOut)
{
    public TimeSpan EnableStart { get; set; }
    public TimeSpan EnableFinish { get; set; }

    public string ToFilter()
    {
        var startTime = Time.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);
        var enableStart = EnableStart.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);
        var enableFinish = EnableFinish.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);

        return $"fade={InOut}:st={startTime}:d=1:alpha=1:enable='between(t,{enableStart},{enableFinish})'";
    }
}

public record FadeInPoint(TimeSpan Time) : FadePoint(Time, "in");

public record FadeOutPoint(TimeSpan Time) : FadePoint(Time, "out");
