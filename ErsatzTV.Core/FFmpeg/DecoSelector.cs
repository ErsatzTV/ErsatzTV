using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.FFmpeg;

public static class DecoSelector
{
    public static DecoEntries GetDecoEntries(Playout playout, DateTimeOffset now)
    {
        if (playout is null)
        {
            return new DecoEntries(Option<Deco>.None, Option<Deco>.None);
        }

        Option<Deco> maybePlayoutDeco = Optional(playout.Deco);
        Option<Deco> maybeTemplateDeco = Option<Deco>.None;

        Option<PlayoutTemplate> maybeActiveTemplate =
            PlayoutTemplateSelector.GetPlayoutTemplateFor(playout.Templates, now);

        foreach (PlayoutTemplate activeTemplate in maybeActiveTemplate)
        {
            Option<DecoTemplateItem> maybeItem = Optional(activeTemplate.DecoTemplate)
                .SelectMany(dt => dt.Items)
                .Find(i => i.StartTime <= now.TimeOfDay && i.EndTime == TimeSpan.Zero || i.EndTime > now.TimeOfDay);
            foreach (DecoTemplateItem item in maybeItem)
            {
                maybeTemplateDeco = Optional(item.Deco);
            }
        }

        return new DecoEntries(maybeTemplateDeco, maybePlayoutDeco);
    }
}

public sealed record DecoEntries(Option<Deco> TemplateDeco, Option<Deco> PlayoutDeco);
