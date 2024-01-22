using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

internal record EffectiveBlock(Block Block, BlockKey BlockKey, DateTimeOffset Start)
{
    public static List<EffectiveBlock> GetEffectiveBlocks(
        ICollection<PlayoutTemplate> templates,
        DateTimeOffset start,
        int daysToBuild)
    {
        DateTimeOffset finish = start.AddDays(daysToBuild);

        var effectiveBlocks = new List<EffectiveBlock>();
        DateTimeOffset current = start.Date;
        while (current < finish)
        {
            Option<PlayoutTemplate> maybeTemplate = PlayoutTemplateSelector.GetPlayoutTemplateFor(templates, current);
            foreach (PlayoutTemplate playoutTemplate in maybeTemplate)
            {
                // logger.LogDebug(
                //     "Will schedule day {Date} using template {Template}",
                //     current,
                //     playoutTemplate.Template.Name);

                DateTimeOffset today = current;
                
                var newBlocks = playoutTemplate.Template.Items
                    .Map(i => ToEffectiveBlock(playoutTemplate, i, today, start))
                    .ToList();

                effectiveBlocks.AddRange(newBlocks);
            }

            current = current.AddDays(1);
        }

        effectiveBlocks.RemoveAll(b => b.Start.AddMinutes(b.Block.Minutes) < start || b.Start > finish);
        effectiveBlocks = effectiveBlocks.OrderBy(rb => rb.Start).ToList();

        return effectiveBlocks;
    }

    private static EffectiveBlock ToEffectiveBlock(
        PlayoutTemplate playoutTemplate,
        TemplateItem templateItem,
        DateTimeOffset current,
        DateTimeOffset start) =>
        new(
            templateItem.Block,
            new BlockKey(templateItem.Block, templateItem.Template, playoutTemplate),
            new DateTimeOffset(
                current.Year,
                current.Month,
                current.Day,
                templateItem.StartTime.Hours,
                templateItem.StartTime.Minutes,
                0,
                start.Offset));
}
