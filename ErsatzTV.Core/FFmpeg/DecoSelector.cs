using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class DecoSelector(ILogger<DecoSelector> logger) : IDecoSelector
{
    public DecoEntries GetDecoEntries(Playout playout, DateTimeOffset now)
    {
        logger.LogDebug("Checking for deco at {Now}", now);

        if (playout is null)
        {
            return new DecoEntries(Option<Deco>.None, Option<Deco>.None);
        }

        Option<Deco> maybePlayoutDeco = Optional(playout.Deco);
        Option<Deco> maybeTemplateDeco = Option<Deco>.None;

        Option<PlayoutTemplate> maybeActiveTemplate =
            AlternateScheduleSelector.GetScheduleForDate(playout.Templates, now);

        foreach (PlayoutTemplate activeTemplate in maybeActiveTemplate)
        {
            Option<DecoTemplateItem> maybeItem = Optional(activeTemplate.DecoTemplate)
                .SelectMany(dt => dt.Items)
                .Find(i => i.StartTime <= now.TimeOfDay && (i.EndTime == TimeSpan.Zero || i.EndTime > now.TimeOfDay));
            foreach (DecoTemplateItem item in maybeItem)
            {
                logger.LogDebug("Selecting deco between {Start} and {End}", item.StartTime, item.EndTime);
                maybeTemplateDeco = Optional(item.Deco);
            }
        }

        return new DecoEntries(maybeTemplateDeco, maybePlayoutDeco);
    }
}
