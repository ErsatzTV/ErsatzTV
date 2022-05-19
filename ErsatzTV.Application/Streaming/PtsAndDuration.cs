namespace ErsatzTV.Application.Streaming;

public record PtsAndDuration(long Pts, long Duration)
{
    public static PtsAndDuration From(string ffprobeLine)
    {
        string[] split = ffprobeLine.Split("|");
        var left = long.Parse(split[0]);
        if (!long.TryParse(split[1], out long right))
        {
            // some durations are N/A, so we have to guess at something
            right = 10_000;
        }

        return new PtsAndDuration(left, right);
    }
}
