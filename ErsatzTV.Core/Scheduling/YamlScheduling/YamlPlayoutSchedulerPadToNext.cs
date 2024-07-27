using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutSchedulerPadToNext : YamlPlayoutSchedulerDuration
{
    public static DateTimeOffset Schedule(
        YamlPlayoutContext context,
        YamlPlayoutPadToNextInstruction padToNext,
        IMediaCollectionEnumerator enumerator,
        Option<IMediaCollectionEnumerator> fallbackEnumerator)
    {
        int currentMinute = context.CurrentTime.Minute;

        int targetMinute = (currentMinute + padToNext.PadToNext - 1) / padToNext.PadToNext * padToNext.PadToNext;

        DateTimeOffset almostTargetTime =
            context.CurrentTime - TimeSpan.FromMinutes(currentMinute) + TimeSpan.FromMinutes(targetMinute);

        var targetTime = new DateTimeOffset(
            almostTargetTime.Year,
            almostTargetTime.Month,
            almostTargetTime.Day,
            almostTargetTime.Hour,
            almostTargetTime.Minute,
            0,
            almostTargetTime.Offset);

        // ensure filler works for content less than one minute
        if (targetTime <= context.CurrentTime)
            targetTime = targetTime.AddMinutes(padToNext.PadToNext);

        return Schedule(
            context,
            targetTime,
            padToNext.DiscardAttempts,
            padToNext.Trim,
            GetFillerKind(padToNext),
            enumerator,
            fallbackEnumerator);
    }
}
