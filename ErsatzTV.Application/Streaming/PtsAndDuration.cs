namespace ErsatzTV.Application.Streaming;

public record PtsAndDuration(long Pts, long Duration)
{
    public static PtsAndDuration From(string ffprobeLine)
    {
        string[] split = ffprobeLine.Split("|");
        var left = long.Parse(split[0]);
        var right = long.Parse(split[1]);
        return new PtsAndDuration(left, right);
    }
}
