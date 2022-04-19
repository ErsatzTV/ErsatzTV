namespace ErsatzTV.Core.FFmpeg;

public static class WatermarkCalculator
{
    public static List<FadePoint> CalculateFadePoints(
        DateTimeOffset itemStartTime,
        TimeSpan inPoint,
        TimeSpan outPoint,
        Option<TimeSpan> streamSeek,
        int frequencyMinutes,
        int durationSeconds)
    {
        var result = new List<FadePoint>();

        TimeSpan duration = outPoint - inPoint;
        DateTimeOffset itemFinishTime = itemStartTime + duration;

        DateTimeOffset start = itemStartTime.AddMinutes(-16);

        // find the next whole minute
        if (start.Second > 0 || start.Millisecond > 0)
        {
            start = start.AddMinutes(1);
            start = start.AddSeconds(-start.Second);
            start = start.AddMilliseconds(-start.Millisecond);
        }

        DateTimeOffset finish = itemFinishTime;

        // find the previous whole minute
        if (finish.Second > 0 || finish.Millisecond > 0)
        {
            finish = finish.AddSeconds(-finish.Second);
            finish = finish.AddMilliseconds(-finish.Millisecond);
        }

        DateTimeOffset current = start;
        while (current <= finish)
        {
            current = current.AddMinutes(1);
            if (current.Minute % frequencyMinutes == 0)
            {
                TimeSpan fadeInTime = inPoint + (current - itemStartTime);

                result.Add(new FadeInPoint(fadeInTime));
                result.Add(new FadeOutPoint(fadeInTime.Add(TimeSpan.FromSeconds(durationSeconds))));
            }
        }

        // if we're seeking, subtract the seek from each item and return that
        foreach (TimeSpan ss in streamSeek)
        {
            result = result.Map(fp => fp with { Time = fp.Time - ss }).ToList();
        }

        // trim points that have already passed
        result.RemoveAll(fp => fp.Time < TimeSpan.Zero);

        // trim points that are past the end
        result.RemoveAll(fp => fp.Time >= outPoint);

        if (result.Any())
        {
            for (var i = 0; i < result.Count; i++)
            {
                result[i].EnableStart = i == 0 ? TimeSpan.Zero : result[i - 1].Time.Add(TimeSpan.FromSeconds(1));
            }

            for (var i = 0; i < result.Count; i++)
            {
                result[i].EnableFinish = i == result.Count - 1
                    ? outPoint
                    : result[i + 1].Time.Subtract(TimeSpan.FromSeconds(1));
            }
        }

        return result;
    }
}
