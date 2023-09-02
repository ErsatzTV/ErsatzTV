namespace ErsatzTV.Core;

public static class SystemTime
{
    public static readonly DateTime MinValueUtc = new(0, DateTimeKind.Utc);
    public static readonly DateTime MaxValueUtc = new(DateTime.MaxValue.Ticks, DateTimeKind.Utc);
}
