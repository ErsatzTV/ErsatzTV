using System;

namespace ErsatzTV.Core
{
    public static class SystemTime
    {
        public static DateTime MinValueUtc = new(0, DateTimeKind.Utc);
        public static DateTime MaxValueUtc = new(DateTime.MaxValue.Ticks, DateTimeKind.Utc);
    }
}
