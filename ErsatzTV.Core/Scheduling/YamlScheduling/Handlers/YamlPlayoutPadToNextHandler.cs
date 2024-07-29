using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutPadToNextHandler(EnumeratorCache enumeratorCache) : YamlPlayoutDurationHandler(enumeratorCache)
{
    public override async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutPadToNextInstruction padToNext)
        {
            return false;
        }

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

        Option<IMediaCollectionEnumerator> maybeEnumerator = await GetContentEnumerator(
            context,
            instruction.Content,
            logger,
            cancellationToken);

        Option<IMediaCollectionEnumerator> fallbackEnumerator = await GetContentEnumerator(
            context,
            padToNext.Fallback,
            logger,
            cancellationToken);

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            context.CurrentTime = Schedule(
                context,
                padToNext.Content,
                padToNext.Fallback,
                targetTime,
                padToNext.DiscardAttempts,
                padToNext.Trim,
                offlineTail: true,
                GetFillerKind(padToNext),
                enumerator,
                fallbackEnumerator);

            return true;
        }

        return false;
    }
}
